// <copyright file="TestWatcher.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify watcher functionality.
    /// </summary>
    [TestClass]
    public sealed class TestWatcher : RingMasterBackendCoreUnitTest
    {
        /// <summary>
        /// Watcher test implementation.
        /// </summary>
        private readonly RingMaster.TestCases.TestWatcher watcherTest = new RingMaster.TestCases.TestWatcher();

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.watcherTest.ConnectToRingMaster = this.ConnectToRingMaster;
            this.watcherTest.Initialize();
        }

        /// <summary>
        /// Verifies that the watcher is notified when children of the watched
        /// nodes change.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestChildrenChangedEvent()
        {
            this.watcherTest.TestChildrenChangedEvent().Wait();
        }

        /// <summary>
        /// Verifies that the reusable watcher is notified when children of the watched
        /// nodes change.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestChildrenChangedEventReusable()
        {
            this.watcherTest.TestChildrenChangedEventReusable().Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when a watched node is deleted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeletedEvent()
        {
            this.watcherTest.TestDeletedEvent().Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when the data of a watched node is changed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDataChangedEvent()
        {
            this.watcherTest.TestDataChangedEvent().Wait();
        }

        /// <summary>
        /// Verify that the bulk watcher is notified when modifications are made under the watched path.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBulkWatcherLegacy()
        {
            this.watcherTest.TestBulkWatcher(RegisterBulkWatcherLegacyMethod).Wait();
        }

        /// <summary>
        /// Verify that multiple sessions can set bulkwatchers on the same path.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBulkWatcherMultipleSessionsLegacy()
        {
            this.watcherTest.TestBulkWatcherMultipleSessions(RegisterBulkWatcherLegacyMethod).Wait();
        }

        /// <summary>
        /// Verify that multiple bulkwatchers can be installed at different points in the path and that
        /// they all get notified.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestMultipleBulkWatchersInPathLegacy()
        {
            this.watcherTest.TestMultipleBulkWatchersInPath(RegisterBulkWatcherLegacyMethod).Wait();
        }

        /// <summary>
        /// Verify that the bulk watcher is notified when modifications are made under the watched path.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBulkWatcher()
        {
            this.watcherTest.TestBulkWatcher(RingMasterExtensions.RegisterBulkWatcher).Wait();
        }

        /// <summary>
        /// Verify that if the same session sets a bulkwatcher on a path that it previously installed a bulk watcher on,
        /// then the original bulkwatcher is replaced iwth the new one.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestReplaceBulkWatcher()
        {
            this.watcherTest.TestReplaceBulkWatcher(RingMasterExtensions.RegisterBulkWatcher).Wait();
        }

        /// <summary>
        /// Verify that if the same session sets a null bulkwatcher on a path that it previously installed a bulk watcher on,
        /// then the original bulkwatcher is removed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestRemoveBulkWatcher()
        {
            this.watcherTest.TestRemoveBulkWatcher(RingMasterExtensions.RegisterBulkWatcher).Wait();
        }

        /// <summary>
        /// Verify that multiple sessions can set bulkwatchers on the same path.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBulkWatcherMultipleSessions()
        {
            this.watcherTest.TestBulkWatcherMultipleSessions(RingMasterExtensions.RegisterBulkWatcher).Wait();
        }

        /// <summary>
        /// Verify that multiple bulkwatchers can be installed at different points in the path and that
        /// they all get notified.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestMultipleBulkWatchersInPath()
        {
            this.watcherTest.TestMultipleBulkWatchersInPath(RingMasterExtensions.RegisterBulkWatcher).Wait();
        }

        /// <summary>
        /// Verify that bulkwatcher cannot be installed on a node that does not exist.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestRegisterBulkWatcherOnNonExistentNode()
        {
            this.watcherTest.TestRegisterBulkWatcherOnNonExistentNode(RingMasterExtensions.RegisterBulkWatcher).Wait();
        }

        private static async Task RegisterBulkWatcherLegacyMethod(IRingMasterRequestHandler ringMaster, string pathPrefix, IWatcher watcher)
        {
            string watcherBody = string.Format("$startswith:{0}", pathPrefix);
            string name = await ringMaster.Create("/$bulkwatcher/watcher", Encoding.UTF8.GetBytes(watcherBody), null, CreateMode.EphemeralSequential);
            string bulkWatcherPath = string.Format("/$bulkwatcher/{0}", name);

            await ringMaster.Exists(bulkWatcherPath, watcher);
        }
    }
}