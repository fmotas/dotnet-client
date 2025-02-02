﻿using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Client.Classes
{
    public class JSONFileClient : SplitClient
    {
        public JSONFileClient(string splitsFilePath,
            string segmentsFilePath,
            ISplitLogger log = null,
            ISegmentCache segmentCacheInstance = null,
            ISplitCache splitCacheInstance = null,
            IImpressionsLog impressionsLog = null,
            bool isLabelsEnabled = true,
            IEventsLog eventsLog = null,
            ITrafficTypeValidator trafficTypeValidator = null,
            IImpressionsManager impressionsManager = null) : base(GetLogger(log))
        {
            _segmentCache = segmentCacheInstance ?? new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            var segmentFetcher = new JSONFileSegmentFetcher(segmentsFilePath, _segmentCache);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher(splitsFilePath);
            var task = splitChangeFetcher.Fetch(-1, new FetchOptions());
            task.Wait();
            
            var splitChangesResult = task.Result;
            var parsedSplits = new ConcurrentDictionary<string, ParsedSplit>();

            _splitParser = new InMemorySplitParser(segmentFetcher, _segmentCache);

            foreach (var split in splitChangesResult.splits)
            {
                parsedSplits.TryAdd(split.name, _splitParser.Parse(split));
            }

            _splitCache = splitCacheInstance ?? new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(parsedSplits));

            _impressionsLog = impressionsLog;

            LabelsEnabled = isLabelsEnabled;

            _eventsLog = eventsLog;
            _trafficTypeValidator = trafficTypeValidator;
            
            _blockUntilReadyService = new NoopBlockUntilReadyService();
            _manager = new SplitManager(_splitCache, _blockUntilReadyService, log);

            ApiKey = "localhost";

            BuildEvaluator(log);

            _uniqueKeysTracker = new NoopUniqueKeysTracker();
            _impressionsCounter = new NoopImpressionsCounter();
            _impressionsObserver = new NoopImpressionsObserver();
            _impressionsManager = impressionsManager ?? new ImpressionsManager(impressionsLog, null, _impressionsCounter, false, ImpressionsMode.Debug, null, _tasksManager, _uniqueKeysTracker, _impressionsObserver);
        }

        #region Public Methods
        public void RemoveSplitFromCache(string splitName)
        {
            _splitCache.RemoveSplit(splitName);
        }

        public void RemoveKeyFromSegmentCache(string segmentName, List<string> keys)
        {
            _segmentCache.RemoveFromSegment(segmentName, keys);
        }

        public override void Destroy()
        {
            if (!_statusManager.IsDestroyed())
            {
                _splitCache.Clear();
                _segmentCache.Clear();
                base.Destroy();
            }
        }
        #endregion

        #region Private Methods
        private static ISplitLogger GetLogger(ISplitLogger splitLogger = null)
        {
            return splitLogger ?? WrapperAdapter.GetLogger(typeof(JSONFileClient));
        }
        #endregion
    }
}
