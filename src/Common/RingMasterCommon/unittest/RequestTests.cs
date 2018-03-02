// <copyright file="RequestTests.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterCommonUnitTest
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This class contains tests for requests
    /// </summary>
    [TestClass]
    public class RequestTests
    {
        /// <summary>
        /// This method tests that read-only determination is correct
        /// </summary>
        [TestMethod]
        public void TestIsReadOnly()
        {
            Assert.IsTrue(new RequestCheck(path: "/", version: 1).IsReadOnly());
            Assert.IsFalse(new RequestCreate(path: "/", data: null, acl: null, createMode: CreateMode.Persistent).IsReadOnly());
            Assert.IsFalse(new RequestDelete(path: "/", version: 1, cascade: false).IsReadOnly());
            Assert.IsTrue(new RequestExists(path: "/", watcher: null).IsReadOnly());
            Assert.IsTrue(new RequestGetAcl(path: "/", stat: null).IsReadOnly());
            Assert.IsTrue(new RequestGetChildren(path: "/", watcher: null, retrievalCondition: null).IsReadOnly());
            Assert.IsTrue(new RequestGetData(path: "/", options: RequestGetData.GetDataOptions.None, watcher: null).IsReadOnly());
            Assert.IsFalse(new RequestInit(sessionId: 0, sessionPwd: "abc", readOnlyInterfaceRequiresLocks: true, redirection: RequestInit.RedirectionPolicy.ServerDefault).IsReadOnly());
            Assert.IsFalse(new RequestSetAcl(path: "/", acl: null, version: -1).IsReadOnly());
            Assert.IsFalse(new RequestSetAuth(clientId: "abc").IsReadOnly());
            Assert.IsFalse(new RequestSetData(path: "/", data: null, version: 1, dataCommand: false).IsReadOnly());
            Assert.IsTrue(new RequestSync(path: "/").IsReadOnly());

            Assert.IsTrue(new RequestMulti(new IRingMasterRequest[0], completeSynchronously: false).IsReadOnly());

            var readMulti = new RequestMulti(
                new List<Op>
                {
                    Op.Check("/", 1),
                    Op.GetData("/a", RequestGetData.GetDataOptions.None, Op.Check("/a", 1))
                },
                completeSynchronously: false);
            Assert.IsTrue(readMulti.IsReadOnly());

            var readWriteMulti = new RequestMulti(
                new List<Op>
                {
                    Op.Check("/a", 1),
                    Op.Create("/a/b", null, null, CreateMode.Ephemeral)
                },
                completeSynchronously: true);
            Assert.IsFalse(readWriteMulti.IsReadOnly());

            var nestedReadMulti = new RequestMulti(
                new IRingMasterRequest[]
                {
                    readMulti
                },
                completeSynchronously: false);
            Assert.IsTrue(nestedReadMulti.IsReadOnly());

            var nestedReadWriteMulti = new RequestMulti(
                new IRingMasterRequest[] 
                {
                    readMulti,
                    readWriteMulti 
                },
                completeSynchronously: true);
            Assert.IsFalse(nestedReadWriteMulti.IsReadOnly());

            Assert.IsTrue(new RequestBatch(new IRingMasterRequest[0], completeSynchronously: false).IsReadOnly());

            var readBatch = new RequestBatch(
                new List<Op>
                {
                    Op.Check("/", 1),
                    Op.GetData("/a", RequestGetData.GetDataOptions.None, Op.Check("/a", 1))
                },
                completeSynchronously: false);
            Assert.IsTrue(readBatch.IsReadOnly());

            var readWriteBatch = new RequestBatch(
                new List<Op>
                {
                    Op.Check("/a", 1),
                    Op.Create("/a/b", null, null, CreateMode.Ephemeral)
                },
                completeSynchronously: false);
            Assert.IsFalse(readWriteBatch.IsReadOnly());

            var nestedReadBatch = new RequestBatch(
                new IRingMasterRequest[]
                {
                    readMulti,
                    readBatch
                },
                completeSynchronously: false);
            Assert.IsTrue(nestedReadBatch.IsReadOnly());

            var nestedReadWriteBatch = new RequestMulti(
                new IRingMasterRequest[]
                {
                    readBatch,
                    readWriteMulti
                },
                completeSynchronously: false);
            Assert.IsFalse(nestedReadWriteBatch.IsReadOnly());
        }
    }
}
