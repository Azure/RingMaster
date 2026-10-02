namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Diagnostics.Tracing;
    using Backend;
    using Data;
    using Requests;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RequestResponseTests
    {
        [TestMethod]
        public void ToStringFast_Is_The_Same_As_ToString()
        {
            var response = CreateFakeResponse(includeStats: true);

            Assert.AreEqual(response.ToString(), response.ToStringFast());
        }
        
        [TestMethod]
        public void ToStringFast_Is_The_Same_As_ToString_No_Stats()
        {
            var response = CreateFakeResponse(includeStats: false);

            Assert.AreEqual(response.ToString(), response.ToStringFast());
        }
        
        private static RequestResponse CreateFakeResponse(bool includeStats) => new RequestResponse()
        {
            CallId = 42,
            ResultCode = (int)RingMasterException.Code.Connectionloss,
            Stat = includeStats ? new Stat(czxid: 42, mzxid: 1, ctime: DateTime.Now.Ticks, version: 42, mtime: 42, cversion: 1,
                aversion: 2, ephemeralOwner: 3, dataLength: 4, numChildren: 1, pzxid: -1, uversion: 7) : null,
        };
    }
}
