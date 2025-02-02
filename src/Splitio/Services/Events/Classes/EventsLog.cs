﻿using Splitio.Domain;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio.Services.Events.Classes
{
    public class EventsLog : IEventsLog
    {
        private static readonly long MAX_SIZE_BYTES = 5 * 1024 * 1024L;
        protected static readonly ISplitLogger Logger = WrapperAdapter.GetLogger(typeof(EventsLog));

        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly IEventSdkApiClient _apiClient;
        private readonly ISimpleProducerCache<WrappedEvent> _wrappedEventsCache;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ITasksManager _tasksManager;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _interval;
        private readonly int _firstPushWindow;
        
        private readonly object _lock = new object();
        private readonly object _sendBulkEventsLock = new object();

        private bool _running;
        private long _acumulateSize;
        
        public EventsLog(IEventSdkApiClient apiClient, 
            int firstPushWindow, 
            int interval, 
            ISimpleCache<WrappedEvent> eventsCache,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ITasksManager tasksManager,
            int maximumNumberOfKeysToCache = -1)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _wrappedEventsCache = (eventsCache as ISimpleProducerCache<WrappedEvent>) ?? new InMemorySimpleCache<WrappedEvent>(new BlockingQueue<WrappedEvent>(maximumNumberOfKeysToCache));
            _apiClient = apiClient;
            _interval = interval;
            _firstPushWindow = firstPushWindow;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _tasksManager = tasksManager;

            _wrapperAdapter = new WrapperAdapter();
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_running) return;

                _running = true;
                _tasksManager.Start(() =>
                {
                    _wrapperAdapter.TaskDelay(_firstPushWindow * 1000).Wait();
                    _tasksManager.StartPeriodic(() => SendBulkEvents(), _interval * 1000, _cancellationTokenSource, "Send Bulk Events.");
                }, new CancellationTokenSource(), "Main Events Log.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                SendBulkEvents();
            }
        }

        public void Log(WrappedEvent wrappedEvent)
        {
            var dropped = _wrappedEventsCache.AddItems(new List<WrappedEvent> { wrappedEvent });

            if (dropped == 0)
            {
                _acumulateSize += wrappedEvent.Size;
            }

            RecordStats(dropped);

            if (_wrappedEventsCache.HasReachedMaxSize() || _acumulateSize >= MAX_SIZE_BYTES)
            {
                SendBulkEvents();
            }
        }

        private void SendBulkEvents()
        {
            lock (_sendBulkEventsLock)
            {
                if (_wrappedEventsCache.IsEmpty()) return;

                if (_wrappedEventsCache.HasReachedMaxSize())
                {
                    Logger.Warn("Split SDK events queue is full. Events may have been dropped. Consider increasing capacity.");
                }

                var wrappedEvents = _wrappedEventsCache.FetchAllAndClear();

                if (wrappedEvents.Count <= 0) return;

                try
                {
                    var events = wrappedEvents
                        .Select(x => x.Event)
                        .ToList();

                    _apiClient.SendBulkEventsTask(events);

                    _acumulateSize = 0;
                }
                catch (Exception e)
                {
                    Logger.Error("Exception caught updating events.", e);
                }
            }
        }

        private void RecordStats(int dropped)
        {
            if (_telemetryRuntimeProducer == null) return;

            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsQueued, 1 - dropped);
            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsDropped, dropped);
        }
    }
}