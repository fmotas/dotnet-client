﻿using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsLog : IImpressionsLog
    {
        protected static readonly ISplitLogger Logger = WrapperAdapter.GetLogger(typeof(ImpressionsLog));

        private readonly IImpressionsSdkApiClient _apiClient;
        private readonly ISimpleProducerCache<KeyImpression> _impressionsCache;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ITasksManager _tasksManager;
        private readonly int _interval;
        private readonly object _lock = new object();

        private bool _running;

        public ImpressionsLog(IImpressionsSdkApiClient apiClient,
            int interval,
            ISimpleCache<KeyImpression> impressionsCache,
            ITasksManager tasksManager,
            int maximumNumberOfKeysToCache = -1)
        {
            _apiClient = apiClient;
            _impressionsCache = (impressionsCache as ISimpleProducerCache<KeyImpression>) ?? new InMemorySimpleCache<KeyImpression>(new BlockingQueue<KeyImpression>(maximumNumberOfKeysToCache));            
            _interval = interval;
            _cancellationTokenSource = new CancellationTokenSource();
            _tasksManager = tasksManager;
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_running) return;

                _running = true;
                _tasksManager.StartPeriodic(() => SendBulkImpressions(), _interval * 1000, _cancellationTokenSource, "Main Impressions Log.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                SendBulkImpressions();
            }
        }

        public int Log(IList<KeyImpression> impressions)
        {
            return _impressionsCache.AddItems(impressions);
        }

        private void SendBulkImpressions()
        {
            if (_impressionsCache.HasReachedMaxSize())
            {
                Logger.Warn("Split SDK impressions queue is full. Impressions may have been dropped. Consider increasing capacity.");
            }

            var impressions = _impressionsCache.FetchAllAndClear();

            if (impressions.Count > 0)
            {
                try
                {
                    _apiClient.SendBulkImpressions(impressions);
                }
                catch (Exception e)
                {
                    Logger.Error("Exception caught updating impressions.", e);
                }
            }
        }
    }
}