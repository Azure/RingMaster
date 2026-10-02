// <copyright file="TestWatcher.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.BVT
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify watcher functionality.
    /// </summary>
    [TestClass]
    public sealed class TestWatcher : RingMasterBVT
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
            this.SetupTest();
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
            this.watcherTest.TestChildrenChangedEvent(false).Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when children of the watched
        /// nodes change and the change include child's info.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestChildrenChangedEventIncludechildChange()
        {
            this.watcherTest.TestChildrenChangedEvent(true).Wait();
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
            this.watcherTest.TestDeletedEvent(false).Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when a watched node is deleted
        /// and the change include child's information.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeletedEventIncludeDataAndChildChange()
        {
            this.watcherTest.TestDeletedEvent(true).Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when the data of a watched node is changed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDataChangedEvent()
        {
            this.watcherTest.TestDataChangedEvent(false).Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when the data of a watched node is changed
        /// and the change include data and stat of changed node.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDataChangedEventIncludeData()
        {
            this.watcherTest.TestDataChangedEvent(true).Wait();
        }

        /// <summary>
        /// Tests the user metadata changed event.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestUserMetadataChangedEvent()
        {
            this.watcherTest.TestDataAndUserMetadataChanged(false).Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when the data of a watched node is changed and the change is delivered
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestUserMetadataChangedEventIncludeData()
        {
            this.watcherTest.TestDataAndUserMetadataChanged(true).Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when the session that set the watcher is terminated.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestWatcherRemovedEvent()
        {
            this.watcherTest.TestWatcherRemovedEvent().Wait();
        }

        /// <summary>
        /// Verify that the bulk watcher is notified when modifications are made under the watched path.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBulkWatcher()
        {
            this.watcherTest.TestBulkWatcher(false).Wait();
        }

        /// <summary>
        /// Verify that the bulk watcher is notified when modifications are made under the watched path
        /// and the change include the parent's data as well as child's info.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBulkWatcherIncludeDataAndChildChange()
        {
            this.watcherTest.TestBulkWatcher(true).Wait();
        }
    }
}