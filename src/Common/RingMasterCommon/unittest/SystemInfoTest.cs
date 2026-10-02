// <copyright file="SystemInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterCommonUnitTest
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class SystemInfoTest
    {
        [TestMethod]
        public void TestSystemInfo()
        {
            var allKeys = (int[])Enum.GetValues(typeof(RingMasterException.Code));
            int[] errorDictionaryKeys = new int[allKeys.Length - 1];
            int index = 0;
            for (int i = 0; i < allKeys.Length; i++)
            {
                if (allKeys[i] != (int)RingMasterException.Code.Ok)
                {
                    errorDictionaryKeys[index] = allKeys[i];
                    index++;
                }
            }

            var requestDictionaryKeys = (ushort[])Enum.GetValues(typeof(RingMasterRequestType));

            SystemInfo info = new SystemInfo();
            this.IsEmptyDictionary(info.ErrorCodeDistribution, errorDictionaryKeys);
            this.IsEmptyDictionary(info.ReceivedRequestDistribution, requestDictionaryKeys);

            info.AddToReceivedRequestDistribution(new RequestExists("/", null));
            info.IncrementRespondedRequestCount();

            int noNode = (int)RingMasterException.Code.Nonode;
            info.AddToErrorDistribution(new Requests.RequestResponse()
            {
                ResultCode = noNode
            });

            Assert.AreEqual(1, info.ReceivedRequestDistribution[(ushort)RingMasterRequestType.Exists]);
            Assert.AreEqual(1, info.RespondedRequestCount);

            Assert.AreEqual(1, info.ErrorCodeDistribution[noNode]);

            byte[] serialized = info.Serialize();
            SystemInfo deserialized = SystemInfo.Deserialize(serialized);

            Assert.AreEqual(info.ReceivedRequestDistribution[(ushort)RingMasterRequestType.Exists], deserialized.ReceivedRequestDistribution[(ushort)RingMasterRequestType.Exists]);
            Assert.AreEqual(info.RespondedRequestCount, deserialized.RespondedRequestCount);
            Assert.AreEqual(info.ErrorCodeDistribution[noNode], deserialized.ErrorCodeDistribution[noNode]);

            info.Reset();

            Assert.AreEqual(0, info.RespondedRequestCount);

            this.IsEmptyDictionary(info.ErrorCodeDistribution, errorDictionaryKeys);
        }

        [TestMethod]
        public async Task StressSystemInfoTest()
        {
            var systemInfo = new SystemInfo();
            var rnd = new Random();
            int testTimeSeconds = 30;
            var clock = Stopwatch.StartNew();

            var resetTask = Task.Run(() =>
            {
                try
                {
                    while (clock.ElapsedMilliseconds < testTimeSeconds * 1000)
                    {
                        systemInfo.Reset();
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.ToString());
                }
            });

            var updateTask = Task.Run(() =>
            {
                try
                {
                    while (clock.ElapsedMilliseconds < testTimeSeconds * 1000)
                    {
                        systemInfo.AddToReceivedRequestDistribution(new RequestExists("/", null));
                        systemInfo.AddToErrorDistribution(new RequestResponse()
                        {
                            ResultCode = rnd.Next(1, 10)
                        });

                        systemInfo.IncrementRespondedRequestCount();
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.ToString());
                }
            });

            var serialization = Task.Run(() =>
            {
                try
                {
                    while (clock.ElapsedMilliseconds < testTimeSeconds * 1000)
                    {
                        var bytes = systemInfo.Serialize();
                        var clone = SystemInfo.Deserialize(bytes);
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.ToString());
                }
            });

            await Task.WhenAll(resetTask, updateTask, serialization);
        }

        private void IsEmptyDictionary<T>(ConcurrentDictionary<T, long> dictionary, T[] keys)
        {
            Assert.AreEqual(keys.Length, dictionary.Count);

            foreach (var key in keys)
            {
                Assert.AreEqual(0, dictionary[key]);
            }
        }
    }
}
