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
            this.watcherTest.TestBulkWatcher().Wait();
        }
    }
}