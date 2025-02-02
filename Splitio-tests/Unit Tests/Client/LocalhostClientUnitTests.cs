﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Shared.Classes;

namespace Splitio_Tests.Unit_Tests.Client
{
    [TestClass]
    public class LocalhostClientUnitTests
    {        
        private readonly string rootFilePath;

        public LocalhostClientUnitTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NETCORE
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        [DeploymentItem(@"Resources\test.splits")]
        public void GetTreatmentShouldReturnControlIfSplitNotFound()
        {
            //Arrange
            var splitClient = new LocalhostClient($"{rootFilePath}test.splits");

            //Act
            var result = splitClient.GetTreatment("test", "test");

            //Assert
            Assert.AreEqual("control", result);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\test.splits")]
        public void GetTreatmentShouldRunAsSingleKeyUsingNullBucketingKey()
        {
            var splitClient = new LocalhostClient($"{rootFilePath}test.splits");
            splitClient.BlockUntilReady(1000);

            //Act
            var key = new Key("test", null);
            var result = splitClient.GetTreatment(key, "other_test_feature");

            //Assert
            Assert.AreEqual(key.bucketingKey, key.matchingKey);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\test.splits")]
        public void TrackShouldNotStoreEvents()
        {
            //Arrange
            var splitClient = new LocalhostClientForTesting($"{rootFilePath}test.splits");
            splitClient.BlockUntilReady(1000);
                      
            //Act
            var result = splitClient.Track("test", "test", "test");

            //Assert
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\test.splits")]
        public void Destroy()
        {
            //Arrange
            var _factoryInstantiationsService = FactoryInstantiationsService.Instance();
            var splitClient = new LocalhostClientForTesting($"{rootFilePath}test.splits");

            //Act
            splitClient.BlockUntilReady(1000);
            splitClient.Destroy();
            var result = ((FactoryInstantiationsService)_factoryInstantiationsService).GetInstantiations();

            //Assert
            Assert.IsTrue(splitClient.IsDestroyed());
            Assert.IsFalse(result.IsEmpty);            
        }
    }
}
