﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using System;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.SegmentFetcher
{
    [TestClass]
    public class ApiSegmentChangeFetcherUnitTests
    {
        [TestMethod]
        public async Task FetchSegmentChangesSuccessfull()
        {
            //Arrange
            var apiClient = new Mock<ISegmentSdkApiClient>();
            apiClient
            .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
            .Returns(Task.FromResult(@"{
                          'name': 'payed',
                          'added': [
                            'abcdz',
                            'bcadz',
                            'xzydz'
                          ],
                          'removed': [],
                          'since': -1,
                          'till': 1470947453877
                        }"));
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            
            //Act
            var result = await apiFetcher.Fetch("payed", -1, new FetchOptions());

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("payed", result.name);
            Assert.AreEqual(-1, result.since);
            Assert.AreEqual(1470947453877, result.till);
            Assert.AreEqual(3, result.added.Count);
            Assert.AreEqual(0, result.removed.Count);
        }

        [TestMethod]
        public async Task FetchSegmentChangesWithExcepionSouldReturnNull()
        {
            var apiClient = new Mock<ISegmentSdkApiClient>();
            apiClient
            .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
            .Throws(new Exception());
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
           
            //Act
            var result = await apiFetcher.Fetch("payed", -1, new FetchOptions());

            //Assert
            Assert.IsNull(result);
        }
    }
}
