﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionsLogUnitTests
    {
        private readonly WrapperAdapter wrapperAdapter = new WrapperAdapter();

        private Mock<IImpressionsSdkApiClient> _apiClientMock;
        private BlockingQueue<KeyImpression> _queue;
        private InMemorySimpleCache<KeyImpression> _impressionsCache;
        private ImpressionsLog _impressionsLog;

        [TestInitialize]
        public void Initialize()
        {
            _apiClientMock = new Mock<IImpressionsSdkApiClient>();
            _queue = new BlockingQueue<KeyImpression>(10);
            _impressionsCache = new InMemorySimpleCache<KeyImpression>(_queue);

            _impressionsLog = new ImpressionsLog(_apiClientMock.Object, 1, _impressionsCache, new TasksManager(wrapperAdapter), 10);
        }

        [TestMethod]
        public void LogSuccessfully()
        {
            //Act
            var impressions = new List<KeyImpression>
            {
                new KeyImpression { keyName = "GetTreatment", feature = "test", treatment = "on", time = 7000, changeNumber = 1, label = "test" }
            };

            _impressionsLog.Log(impressions);

            //Assert
            KeyImpression element = null;
            while (element == null)
            {
                element = _queue.Dequeue();
            }
            Assert.IsNotNull(element);
            Assert.AreEqual("GetTreatment", element.keyName);
            Assert.AreEqual("test", element.feature);
            Assert.AreEqual("on", element.treatment);
            Assert.AreEqual(7000, element.time);
        }

        [TestMethod]
        public void LogSuccessfullyUsingBucketingKey()
        {
            //Act
            Key key = new Key(bucketingKey: "a", matchingKey: "testkey");

            var impressions = new List<KeyImpression>
            {
                new KeyImpression { keyName = key.matchingKey, feature = "test", treatment = "on", time = 7000, changeNumber = 1, label = "test-label", bucketingKey = key.bucketingKey }
            };

            _impressionsLog.Log(impressions);

            //Assert
            KeyImpression element = null;
            while (element == null)
            {
                element = _queue.Dequeue();
            }
            Assert.IsNotNull(element);
            Assert.AreEqual("testkey", element.keyName);
            Assert.AreEqual("a", element.bucketingKey);
            Assert.AreEqual("test", element.feature);
            Assert.AreEqual("on", element.treatment);
            Assert.AreEqual(7000, element.time);
        }

        [TestMethod]
        public void LogSuccessfullyAndSendImpressions()
        {
            //Act            
            var impressions = new List<KeyImpression>
            {
                new KeyImpression() { keyName = "GetTreatment", feature = "test", treatment = "on", time = 7000, changeNumber = 1, label = "test-label" }
            };

            _impressionsLog.Start();
            _impressionsLog.Log(impressions);

            //Assert
            Thread.Sleep(2000);
            _apiClientMock.Verify(x => x.SendBulkImpressions(It.Is<List<KeyImpression>>(list => list.Count == 1)));
        }
    }
}
