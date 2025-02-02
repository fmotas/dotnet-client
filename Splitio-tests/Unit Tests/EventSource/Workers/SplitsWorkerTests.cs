﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.EventSource.Workers
{
    [TestClass]
    public class SplitsWorkerTests
    {
        private readonly WrapperAdapter wrapperAdapter = new WrapperAdapter();

        private readonly Mock<ISplitLogger> _log;
        private readonly Mock<ISynchronizer> _synchronizer;
        private readonly Mock<ISplitCache> _splitCache;

        private readonly ISplitsWorker _splitsWorker;

        public SplitsWorkerTests()
        {
            _log = new Mock<ISplitLogger>();
            _synchronizer = new Mock<ISynchronizer>();
            _splitCache = new Mock<ISplitCache>();

            _splitsWorker = new SplitsWorker(_splitCache.Object, _synchronizer.Object, new TasksManager(wrapperAdapter), _log.Object);
        }

        [TestMethod]        
        public void AddToQueue_WithElements_ShouldTriggerFetch()
        {
            // Act.
            _splitsWorker.Start();
            _splitsWorker.AddToQueue(1585956698457);
            _splitsWorker.AddToQueue(1585956698467);
            _splitsWorker.AddToQueue(1585956698477);
            _splitsWorker.AddToQueue(1585956698476);
            Thread.Sleep(1000);

            _splitsWorker.Stop();
            _splitsWorker.AddToQueue(1585956698486);
            Thread.Sleep(100);
            _splitsWorker.AddToQueue(1585956698496);

            // Assert
            _synchronizer.Verify(mock => mock.SynchronizeSplits(It.IsAny<long>()), Times.Exactly(4));
        }

        [TestMethod]
        public void AddToQueue_WithoutElemts_ShouldNotTriggerFetch()
        {
            // Act.
            _splitsWorker.Start();
            Thread.Sleep(500);

            // Assert.
            _splitCache.Verify(mock => mock.GetChangeNumber(), Times.Never);
            _synchronizer.Verify(mock => mock.SynchronizeSplits(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public void Kill_ShouldTriggerFetch()
        {
            // Arrange.            
            var changeNumber = 1585956698457;
            var splitName = "split-test";
            var defaultTreatment = "off";

            _splitCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(1585956698447);

            _splitsWorker.Start();

            // Act.            
            _splitsWorker.KillSplit(changeNumber, splitName, defaultTreatment);
            Thread.Sleep(1000);

            // Assert.
            _splitCache.Verify(mock => mock.Kill(changeNumber, splitName, defaultTreatment), Times.Once);
        }

        [TestMethod]
        public void Kill_ShouldNotTriggerFetch()
        {
            // Arrange.            
            var changeNumber = 1585956698457;
            var splitName = "split-test";
            var defaultTreatment = "off";

            _splitCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(1585956698467);

            _splitsWorker.Start();

            // Act.            
            _splitsWorker.KillSplit(changeNumber, splitName, defaultTreatment);
            Thread.Sleep(1000);

            // Assert.
            _splitCache.Verify(mock => mock.Kill(changeNumber, splitName, defaultTreatment), Times.Never);
        }
    }
}
