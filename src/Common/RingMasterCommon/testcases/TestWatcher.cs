// <copyright file="TestWatcher.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases
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
    public sealed class TestWatcher : RingMasterInterfaceTest
    {
        /// <summary>
        /// The prefix which this test will use to create nodes
        /// </summary>
        private const string TestPrefix = "/$rmbvt/TestWatcher";

        /// <summary>
        /// Initializes the test.
        /// </summary>
        public void Initialize()
        {
            Task.Run(async () =>
            {
                using (var ringMaster = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Initializing TestRoot {0}", TestWatcher.TestPrefix);
                    try
                    {
                        await ringMaster.Create(TestWatcher.TestPrefix, null, null, CreateMode.PersistentAllowPathCreation);
                    }
                    catch (RingMasterException ex)
                    {
                        Assert.AreEqual(RingMasterException.Code.Nodeexists, ex.ErrorCode);
                    }
                }
            }).Wait();
        }

        /// <summary>
        /// Verifies that the watcher is notified when children of the watched
        /// nodes change.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestChildrenChangedEvent(bool includeChildChange)
        {
            var kind = includeChildChange ? WatcherKind.IncludeDataAndChildChange : 0;
            Task<WatchedEvent> createWatcherTask;
            var createWatcher = CreateWatcher(out createWatcherTask, kind);
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = string.Format("{0}/TestChildrenChangedEvent_{1}", TestWatcher.TestPrefix, Guid.NewGuid());

                Trace.TraceInformation("Creating Node {0}", nodePath);
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Trace.TraceInformation("Setting watch on Node {0}", nodePath);
                var stat = await ringMaster.Exists(nodePath, createWatcher);

                string childNodePath = string.Format("{0}/ChildNode", nodePath);

                // Make the modification that triggers the watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Creating child node {0}", childNodePath);
                    await ringMaster2.Create(childNodePath, Guid.NewGuid().ToByteArray(), null, CreateMode.Persistent, Guid.NewGuid().ToByteArray());
                }

                Trace.TraceInformation("Waiting for watcher to receive the NodeChildrenChanged event");
                WatchedEvent createdEvent = await createWatcherTask;

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, createdEvent.EventType);
                Assert.AreEqual(nodePath, createdEvent.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, createdEvent.KeeperState);
                if (includeChildChange)
                {
                    Assert.AreEqual("ChildNode", createdEvent.ChildName);
                    Assert.IsNotNull(createdEvent.ChildData);
                    Assert.IsNotNull(createdEvent.ChildUserMetadata);
                    Assert.IsNotNull(createdEvent.ChildStat);
                }
                else
                {
                    Assert.IsNull(createdEvent.ChildData);
                    Assert.IsNull(createdEvent.ChildUserMetadata);
                    Assert.IsNull(createdEvent.ChildStat);
                    Assert.IsNull(createdEvent.ChildName);
                }

                Task<WatchedEvent> deleteWatcherTask;
                var deleteWatcher = CreateWatcher(out deleteWatcherTask, kind);

                Trace.TraceInformation("Setting watch on Node {0}", nodePath);
                await ringMaster.GetChildren(nodePath, deleteWatcher);

                // Make the modification that triggers the watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Deleting child node {0}", childNodePath);
                    await ringMaster2.Delete(childNodePath, 1);
                }

                Trace.TraceInformation("Waiting for delete watcher to receive the NodeChildrenChanged event");
                var deletedEvent = await deleteWatcherTask;

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, deletedEvent.EventType);
                Assert.AreEqual(nodePath, deletedEvent.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, deletedEvent.KeeperState);
                Assert.IsNull(deletedEvent.ChildStat);
                Assert.IsNull(deletedEvent.ChildData);
                Assert.IsNull(deletedEvent.ChildUserMetadata);
            }
        }

        /// <summary>
        /// Verifies that the reusable watcher is notified when children of the watched
        /// nodes change.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestChildrenChangedEventReusable()
        {
            Task<WatchedEvent>[] createWatcherTasks = new Task<WatchedEvent>[4];
            var createWatcher = CreateWatcher(createWatcherTasks);

            string nodePath = string.Format("{0}/TestChildrenChangedEventReusable_{1}", TestWatcher.TestPrefix, Guid.NewGuid());

            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation("Creating Node {0}", nodePath);
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Trace.TraceInformation("Setting watch on Node {0}", nodePath);
                var stat = await ringMaster.Exists(nodePath, createWatcher);

                string childNodePath = string.Format("{0}/ChildNode", nodePath);

                // Make the modification that triggers the watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Creating child node {0}", childNodePath);
                    await ringMaster2.Create(childNodePath, null, null, CreateMode.Persistent);
                }

                Trace.TraceInformation("Waiting for watcher to receive the NodeChildrenChanged event");
                WatchedEvent createdEvent = await createWatcherTasks[0];

                // Make the modification that triggers the watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Deleting child node {0}", childNodePath);
                    await ringMaster2.Delete(childNodePath, 1);
                }

                Trace.TraceInformation("Waiting for watcher2 to receive the NodeChildrenChanged event");
                WatchedEvent created2Event = await createWatcherTasks[1];

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, created2Event.EventType);
                Assert.AreEqual(nodePath, created2Event.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, created2Event.KeeperState);

                Trace.TraceInformation("Deleting Node {0}", nodePath);
                await ringMaster.Delete(nodePath, -1);

                Trace.TraceInformation("Waiting for watcher3 to receive the NodeDeleted event");
                WatchedEvent created3Event = await createWatcherTasks[2];

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDeleted, created3Event.EventType);
                Assert.AreEqual(nodePath, created3Event.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, created3Event.KeeperState);

                Trace.TraceInformation("Waiting for watcher4 to receive the WatcherRemoved event");
                WatchedEvent created4Event = await createWatcherTasks[3];

                Assert.AreEqual(WatchedEvent.WatchedEventType.WatcherRemoved, created4Event.EventType);
                Assert.AreEqual(nodePath, created4Event.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, created4Event.KeeperState);
            }
        }

        /// <summary>
        /// Verifies that the watcher is notified when a watched node is deleted.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestDeletedEvent(bool includeChildData)
        {
            Task<WatchedEvent> watchTask;
            var kind = includeChildData ? WatcherKind.OneUse | WatcherKind.IncludeDataAndChildChange : WatcherKind.OneUse;
            var watcher = CreateWatcher(out watchTask, kind);
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = string.Format("{0}/TestDeletedEvent_{1}", TestWatcher.TestPrefix, Guid.NewGuid());

                Trace.TraceInformation("Creating Node {0}", nodePath);
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Trace.TraceInformation("Setting watch on Node {0}", nodePath);
                var stat = await ringMaster.Exists(nodePath, watcher);
                
                // Make the modification that triggers the watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Deleting node {0}", nodePath);
                    await ringMaster2.Delete(nodePath, stat.Version);
                }

                Trace.TraceInformation("Waiting for watcher to receive the NodeDeleted event");
                WatchedEvent watchedEvent = await watchTask;

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDeleted, watchedEvent.EventType);
                Assert.AreEqual(nodePath, watchedEvent.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, watchedEvent.KeeperState);
                Assert.IsNull(watchedEvent.Stat);
                Assert.IsNull(watchedEvent.ChildName);
                Assert.IsNull(watchedEvent.ChildStat);
            }
        }

        /// <summary>
        /// Verifies that the watcher is notified when the data of a watched node is changed.
        /// </summary>
        /// <param name="includeDataAndChildChange">Whether to include data in the watched event or not</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestDataChangedEvent(bool includeDataAndChildChange)
        {
            Task<WatchedEvent> watchTask;
            var kind = includeDataAndChildChange ? WatcherKind.OneUse | WatcherKind.IncludeDataAndChildChange : WatcherKind.OneUse;
            var watcher = CreateWatcher(out watchTask, kind);
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = string.Format("{0}/TestDataChangedEvent_{1}", TestWatcher.TestPrefix, Guid.NewGuid());

                Trace.TraceInformation("Creating Node {0}", nodePath);
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Trace.TraceInformation("Setting watch on Node {0}", nodePath);
                await ringMaster.GetData(nodePath, watcher);

                // Make the modification that triggers the watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Changing data for node {0}", nodePath);
                    byte[] nodeData = Guid.NewGuid().ToByteArray();
                    await ringMaster2.SetData(nodePath, nodeData, 1);
                }

                Trace.TraceInformation("Waiting for watcher to receive the NodeDataChanged event");
                WatchedEvent watchedEvent = await watchTask;

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDataChanged, watchedEvent.EventType);
                Assert.AreEqual(nodePath, watchedEvent.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, watchedEvent.KeeperState);
                if (includeDataAndChildChange)
                {
                    Assert.IsNotNull(watchedEvent.Data, "Data should not be null in the watcher return");
                    Assert.IsNotNull(watchedEvent.Stat, "Stat should not be null in the watcher return");
                }
                else
                {
                    Assert.IsNull(watchedEvent.Data, $"Data should be null, actual length {watchedEvent.Data?.Length}");
                    Assert.IsNull(watchedEvent.Stat, $"Stat should be null, actual: {watchedEvent.Stat}");
                }

                Assert.IsNull(watchedEvent.UserMetadata);
                Assert.IsNull(watchedEvent.ChildUserMetadata);
                Assert.IsNull(watchedEvent.ChildName);
                Assert.IsNull(watchedEvent.ChildStat);
                Assert.IsNull(watchedEvent.ChildData);
            }
        }

        /// <summary>
        /// Tests the data and user metadata changed.
        /// </summary>
        /// <param name="includeDataAndChildChange">if set to <c>true</c> [include data and child change].</param>
        /// <returns>async task</returns>
        public async Task TestDataAndUserMetadataChanged(bool includeDataAndChildChange)
        {
            Task<WatchedEvent> watchTask;
            var kind = includeDataAndChildChange ? WatcherKind.OneUse | WatcherKind.IncludeDataAndChildChange : WatcherKind.OneUse;
            var watcher = CreateWatcher(out watchTask, kind);
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = string.Format("{0}/TestDataAndUserMetadataChangedEvent_{1}", TestWatcher.TestPrefix, Guid.NewGuid());

                Trace.TraceInformation("Creating Node {0}", nodePath);
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Trace.TraceInformation("Setting watch on Node {0}", nodePath);
                await ringMaster.GetData(nodePath, watcher);

                // Make the modification that triggers the watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Changing data for node {0}", nodePath);
                    byte[] nodeData = Guid.NewGuid().ToByteArray();
                    await ringMaster2.SetDataAndUserMetadata(nodePath, nodeData, 1, Guid.NewGuid().ToByteArray(), -1);
                }

                Trace.TraceInformation("Waiting for watcher to receive the NodeDataAndUserMetadataChanged event");
                WatchedEvent watchedEvent = await watchTask;

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDataAndUserMetadataChanged, watchedEvent.EventType);
                Assert.AreEqual(nodePath, watchedEvent.Path);
                Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.SyncConnected, watchedEvent.KeeperState);
                if (includeDataAndChildChange)
                {
                    Assert.IsNotNull(watchedEvent.UserMetadata, "User Metadata should not be null in the watcher return");
                    Assert.IsNotNull(watchedEvent.Data, "Data should not be null in the watcher return");
                    Assert.IsNotNull(watchedEvent.Stat, "Stat should not be null in the watcher return");
                }
                else
                {
                    Assert.IsNull(watchedEvent.UserMetadata, $"User Metadata should be null, actual length {watchedEvent.UserMetadata?.Length}");
                    Assert.IsNull(watchedEvent.Data, $"Data should be null, actual length {watchedEvent.Data?.Length}");
                    Assert.IsNull(watchedEvent.Stat, $"Stat should be null, actual: {watchedEvent.Stat}");
                }

                Assert.IsNull(watchedEvent.ChildUserMetadata);
                Assert.IsNull(watchedEvent.ChildName);
                Assert.IsNull(watchedEvent.ChildStat);
                Assert.IsNull(watchedEvent.ChildData);
            }
        }

        /// <summary>
        /// Verifies that the watcher is notified when the session that set the watcher is terminated.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestWatcherRemovedEvent()
        {
            Task<WatchedEvent> watcherTask;
            var watcher = CreateWatcher(out watcherTask);
            string nodePath = string.Format("{0}/TestWatcherRemovedEvent_{1}", TestWatcher.TestPrefix, Guid.NewGuid());
            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation("Creating Node {0}", nodePath);
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Trace.TraceInformation("Setting watch on Node {0}", nodePath);
                var stat = await ringMaster.Exists(nodePath, watcher);
            }

            Trace.TraceInformation("The watcher must have received the WatcherRemoved event");
            var watchedEvent = await watcherTask;

            Assert.AreEqual(WatchedEvent.WatchedEventType.WatcherRemoved, watchedEvent.EventType);
            Assert.AreEqual(nodePath, watchedEvent.Path);
            Assert.AreEqual(WatchedEvent.WatchedEventKeeperState.Disconnected, watchedEvent.KeeperState);
        }

        /// <summary>
        /// Verify that the bulk watcher is notified when modifications are made under the watched path.
        /// </summary>
        /// <param name="registerBulkWatcher">The function to invoke to register a bulkwatcher</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task TestBulkWatcher(bool includeDataAndChildChange, Func<IRingMasterRequestHandler, string, IWatcher, Task> registerBulkWatcher = null)
        {
            var kind = includeDataAndChildChange ? WatcherKind.IncludeDataAndChildChange : default(WatcherKind);
            Task<WatchedEvent>[] bulkWatcherTasks = new Task<WatchedEvent>[4];
            IWatcher watcher = CreateWatcher(bulkWatcherTasks, kind);

            registerBulkWatcher = registerBulkWatcher ?? RingMasterExtensions.RegisterBulkWatcher;

            string nodePath = $"{TestWatcher.TestPrefix}/TestBulkWatcher_{Guid.NewGuid()}";
            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation($"Creating Node {nodePath}");
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Trace.TraceInformation($"Setting bulkwatcher on path prefix {nodePath}");
                await registerBulkWatcher(ringMaster, nodePath, watcher);

                // Make modifications that trigger the bulk watcher from another session.
                using (var ringMaster2 = this.ConnectToRingMaster())
                {
                    string childNodePath = $"{nodePath}/Child1";
                    Trace.TraceInformation($"Creating child node path={childNodePath}");
                    await ringMaster2.Create(childNodePath, Guid.NewGuid().ToByteArray(), null, CreateMode.Persistent, Guid.NewGuid().ToByteArray());

                    Trace.TraceInformation("Waiting for first event");
                    var firstEvent = await bulkWatcherTasks[0];
                    WatchedEvent secondEvent = null;

                    if (includeDataAndChildChange)
                    {
                        Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, firstEvent.EventType);
                        Assert.AreEqual(nodePath, firstEvent.Path);

                        Assert.IsNull(firstEvent.Data);
                        Assert.IsNotNull(firstEvent.Stat);
                        Assert.IsNotNull(firstEvent.ChildUserMetadata);
                        Assert.AreEqual("Child1", firstEvent.ChildName);
                    }
                    else
                    {
                        Trace.TraceInformation("Waiting for second event");
                        secondEvent = await bulkWatcherTasks[1];

                        Assert.AreEqual(WatchedEvent.WatchedEventType.NodeCreated, firstEvent.EventType);
                        Assert.AreEqual(childNodePath, firstEvent.Path);
                        Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, secondEvent.EventType);
                        Assert.AreEqual(nodePath, secondEvent.Path);
                        Assert.IsNull(firstEvent.Data);
                        Assert.IsNull(firstEvent.Stat);
                        Assert.IsNull(firstEvent.ChildName);
                        Assert.IsNull(firstEvent.ChildUserMetadata);
                    }

                    string grandChildNodePath = $"{childNodePath}/GrandChild1";
                    Trace.TraceInformation($"Creating grandchild node path={grandChildNodePath}");
                    await ringMaster2.Create(grandChildNodePath, null, null, CreateMode.Persistent);

                    if (includeDataAndChildChange)
                    {
                        Trace.TraceInformation("Waiting for NodeCreated event");
                        firstEvent = await bulkWatcherTasks[1];

                        Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, firstEvent.EventType);
                        Assert.AreEqual(childNodePath, firstEvent.Path);
                        Assert.IsNotNull(firstEvent.Stat);
                        Assert.IsNotNull(firstEvent.ChildName);
                        Assert.IsNotNull(firstEvent.ChildStat);
                    }
                    else
                    {
                        Trace.TraceInformation("Waiting for NodeCreated event");
                        firstEvent = await bulkWatcherTasks[2];
                        Trace.TraceInformation("Waiting for NodeChildrenChanged event");
                        secondEvent = await bulkWatcherTasks[3];

                        Assert.AreEqual(WatchedEvent.WatchedEventType.NodeCreated, firstEvent.EventType);
                        Assert.AreEqual(grandChildNodePath, firstEvent.Path);
                        Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, secondEvent.EventType);
                        Assert.IsNull(secondEvent.Stat);
                        Assert.AreEqual(childNodePath, secondEvent.Path);
                        Assert.IsNull(secondEvent.ChildName);
                        Assert.IsNull(secondEvent.ChildData);
                        Assert.IsNull(secondEvent.ChildStat);
                    }

                    Trace.TraceInformation("Closing ringMaster2");
                }

                Trace.TraceInformation("Closing ringMaster1");
            }
        }

        /// <summary>
        /// Verify that if the same session sets a bulkwatcher on a path that it previously installed a bulk watcher on,
        /// then the original bulkwatcher is replaced with the new one.
        /// </summary>
        /// <param name="registerBulkWatcher">The function to invoke to register a bulkwatcher</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task TestReplaceBulkWatcher(Func<IRingMasterRequestHandler, string, IWatcher, Task> registerBulkWatcher)
        {
            string nodePath = $"{TestWatcher.TestPrefix}/TestReplaceBulkWatcher_{Guid.NewGuid()}";
            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation($"Creating Node {nodePath}");
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Task<WatchedEvent> watcher1Notified;
                var watcher1 = CreateWatcher(out watcher1Notified);

                Trace.TraceInformation($"Setting bulkwatcher on path prefix {nodePath}");
                await registerBulkWatcher(ringMaster, nodePath, watcher1);

                Task<WatchedEvent>[] watcher2Notifications = new Task<WatchedEvent>[2];
                var watcher2 = CreateWatcher(watcher2Notifications);

                Trace.TraceInformation($"Replacing bulkwatcher on path prefix {nodePath}");
                await registerBulkWatcher(ringMaster, nodePath, watcher2);

                WatchedEvent watcher1Notification = await watcher1Notified;
                Assert.AreEqual(WatchedEvent.WatchedEventType.WatcherRemoved, watcher1Notification.EventType);

                string childPath = $"{nodePath}/Child";
                await ringMaster.Create(childPath, null, null, CreateMode.Ephemeral);

                await Task.WhenAll(watcher2Notifications);

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeCreated, watcher2Notifications[0].Result.EventType);
                Assert.AreEqual(childPath, watcher2Notifications[0].Result.Path);

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, watcher2Notifications[1].Result.EventType);
                Assert.AreEqual(nodePath, watcher2Notifications[1].Result.Path);
            }
        }

        /// <summary>
        /// Verify that if the same session sets a null bulkwatcher on a path that it previously installed a bulk watcher on,
        /// then the original bulkwatcher is removed.
        /// </summary>
        /// <param name="registerBulkWatcher">The function to invoke to register a bulkwatcher</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task TestRemoveBulkWatcher(Func<IRingMasterRequestHandler, string, IWatcher, Task> registerBulkWatcher)
        {
            string nodePath = $"{TestWatcher.TestPrefix}/TestRemoveBulkWatcher{Guid.NewGuid()}";
            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation($"Creating Node {nodePath}");
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Task<WatchedEvent> watcher1Notified;
                var watcher1 = CreateWatcher(out watcher1Notified);

                Trace.TraceInformation($"Setting bulkwatcher on path prefix {nodePath}");
                await registerBulkWatcher(ringMaster, nodePath, watcher1);

                Task<WatchedEvent>[] watcher2Notifications = new Task<WatchedEvent>[2];
                var watcher2 = CreateWatcher(watcher2Notifications);

                Trace.TraceInformation($"Removing bulkwatcher on path prefix {nodePath}");
                await registerBulkWatcher(ringMaster, nodePath, null);

                WatchedEvent watcher1Notification = await watcher1Notified;
                Assert.AreEqual(WatchedEvent.WatchedEventType.WatcherRemoved, watcher1Notification.EventType);
            }
        }

        /// <summary>
        /// Verify that multiple sessions can set bulkwatchers on the same path.
        /// </summary>
        /// <param name="registerBulkWatcher">The function to invoke to register a bulkwatcher</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task TestBulkWatcherMultipleSessions(Func<IRingMasterRequestHandler, string, IWatcher, Task> registerBulkWatcher)
        {
            Task<WatchedEvent>[] session1Notifications = new Task<WatchedEvent>[2];
            Task<WatchedEvent>[] session2Notifications = new Task<WatchedEvent>[1];

            string nodePath = $"{TestWatcher.TestPrefix}/TestBulkWatcherMultipleSession{Guid.NewGuid()}";
            using (var session1 = this.ConnectToRingMaster())
            {
                Trace.TraceInformation($"Creating Node {nodePath}");
                await session1.Create(nodePath, null, null, CreateMode.Persistent);

                var session1Watcher = CreateWatcher(session1Notifications);

                Trace.TraceInformation($"Setting session1 bulkwatcher on path prefix {nodePath}");
                await registerBulkWatcher(session1, nodePath, session1Watcher);

                using (var session2 = this.ConnectToRingMaster())
                {
                    var session2Watcher = CreateWatcher(session2Notifications);

                    Trace.TraceInformation($"Setting session2 bulkwatcher on path prefix {nodePath}");
                    await registerBulkWatcher(session2, nodePath, session2Watcher);

                    Trace.TraceInformation($"Modifying data for path {nodePath}");
                    await session2.SetData(nodePath, Guid.NewGuid().ToByteArray(), -1);

                    Trace.TraceInformation($"Waiting for notifications from watchers installed by both sessions");
                    await session1Notifications[0];
                    await session2Notifications[0];

                    Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDataChanged, session1Notifications[0].Result.EventType);
                    Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDataChanged, session2Notifications[0].Result.EventType);
                }

                Trace.TraceInformation($"Modifying data for path {nodePath}");
                await session1.SetData(nodePath, Guid.NewGuid().ToByteArray(), -1);

                Trace.TraceInformation("Waiting for NodeDataChanged notification from session1 watcher");
                await session1Notifications[1];

                Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDataChanged, session1Notifications[0].Result.EventType);
            }
        }

        /// <summary>
        /// Verify that multiple bulkwatchers can be installed at different points in the path and that
        /// they all get notified.
        /// </summary>
        /// <param name="registerBulkWatcher">The function to invoke to register a bulkwatcher</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task TestMultipleBulkWatchersInPath(Func<IRingMasterRequestHandler, string, IWatcher, Task> registerBulkWatcher)
        {
            Task<WatchedEvent>[] watchedEvents = new Task<WatchedEvent>[5];

            string nodePath = $"{TestWatcher.TestPrefix}/TestMultipleBulkWatchersInPath{Guid.NewGuid()}";
            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation($"Creating Node {nodePath}");
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                string path = nodePath;
                for (int i = 0; i < 5; i++)
                {
                    path = $"{path}/{i}";
                    Trace.TraceInformation($"Creating Node {path}");
                    await ringMaster.Create(path, null, null, CreateMode.Persistent);
                }

                path = nodePath;
                for (int i = 0; i < 5; i++)
                {
                    path = $"{path}/{i}";
                    Trace.TraceInformation($"Registering bulk watcher for {path}");
                    var watcher = CreateWatcher(out watchedEvents[i]);
                    await registerBulkWatcher(ringMaster, path, watcher);
                }

                Trace.TraceInformation($"Modifying data for path {path}");
                await ringMaster.SetData(path, Guid.NewGuid().ToByteArray(), -1);

                Trace.TraceInformation($"Waiting for all watchers to be notified");
                await Task.WhenAll(watchedEvents);

                for (int i = 0; i < 5; i++)
                {
                    Assert.AreEqual(WatchedEvent.WatchedEventType.NodeDataChanged, watchedEvents[i].Result.EventType);
                    Assert.AreEqual(path, watchedEvents[i].Result.Path);
                }
            }
        }

        /// <summary>
        /// Verify that bulkwatcher cannot be installed on a node that does not exist.
        /// </summary>
        /// <param name="registerBulkWatcher">The function to invoke to register a bulkwatcher</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task TestRegisterBulkWatcherOnNonExistentNode(Func<IRingMasterRequestHandler, string, IWatcher, Task> registerBulkWatcher)
        {
            Task<WatchedEvent>[] watchedEvents = new Task<WatchedEvent>[5];

            string nodePath = $"{TestWatcher.TestPrefix}/TestRegisterBulkWatcherOnNonExistentNode{Guid.NewGuid()}";
            using (var ringMaster = this.ConnectToRingMaster())
            {
                Task<WatchedEvent> watcherNotified;
                var watcher = CreateWatcher(out watcherNotified);

                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    () => registerBulkWatcher(ringMaster, nodePath, watcher),
                    "Bulk watcher registration on non-existent node must fail");
            }
        }

        /// <summary>
        /// Create a watcher that completes a task when notified.
        /// </summary>
        /// <param name="watchedEventTask">The task that will be completed when the watcher is notified</param>
        /// <param name="kind">Kind of the watcher</param>
        /// <returns>Interface to the watcher</returns>
        private static IWatcher CreateWatcher(out Task<WatchedEvent> watchedEventTask, WatcherKind kind = WatcherKind.OneUse)
        {
            var watcherTaskCompletionSource = new TaskCompletionSource<WatchedEvent>();
            var watcher = new DelegateWatcher(
                watchedEvent => ThreadPool.QueueUserWorkItem(_ => watcherTaskCompletionSource.TrySetResult(watchedEvent)),
                kind);

            watchedEventTask = watcherTaskCompletionSource.Task;

            return watcher;
        }

        /// <summary>
        /// Create a reusable watcher that completes a number of tasks as it is notified.
        /// </summary>
        /// <param name="watchedEventTaskArray">The array of tasks that will be completed when the watcher is notified</param>
        /// <returns>Interface to the watcher</returns>
        private static IWatcher CreateWatcher(Task<WatchedEvent>[] watchedEventTaskArray, WatcherKind kind = default(WatcherKind))
        {
            Queue<TaskCompletionSource<WatchedEvent>> srcArray = new Queue<TaskCompletionSource<WatchedEvent>>();

            for (int i = 0; i < watchedEventTaskArray.Length; i++)
            {
                TaskCompletionSource<WatchedEvent> item = new TaskCompletionSource<WatchedEvent>();
                srcArray.Enqueue(item);
                watchedEventTaskArray[i] = item.Task;
            }

            var watcher = new DelegateWatcher(
                watchedEvent =>
                {
                    TaskCompletionSource<WatchedEvent> item = null;

                    // notify the first source in the queue
                    lock (srcArray)
                    {
                        Trace.TraceInformation($"DelegateWatcher: WatchedEvent eventType={watchedEvent.EventType}, path={watchedEvent.Path}, srcArrayCount={srcArray.Count}");
                        if (srcArray.Count == 0)
                        {
                            return;
                        }

                        item = srcArray.Dequeue();
                    }

                    ThreadPool.QueueUserWorkItem(_ => item.TrySetResult(watchedEvent));
                },
                kind);

            return watcher;
        }

        /// <summary>
        /// A watcher that invokes a delegate when notified.
        /// </summary>
        private class DelegateWatcher : IWatcher
        {
            /// <summary>
            /// The delegate to be invoked.
            /// </summary>
            private Action<WatchedEvent> watchedEventDelegate;

            /// <summary>
            /// Initializes a new instance of the <see cref="DelegateWatcher"/> class.
            /// </summary>
            /// <param name="watchedEventDelegate">The delegate to be invoked</param>
            /// <param name="kind">Kind of the watcher</param>
            /// <exception cref="System.ArgumentNullException">Thrown if onProcess is null</exception>
            public DelegateWatcher(Action<WatchedEvent> watchedEventDelegate, WatcherKind kind = WatcherKind.OneUse)
            {
                this.Kind = kind;
                this.watchedEventDelegate = watchedEventDelegate;
                if (watchedEventDelegate == null)
                {
                    throw new ArgumentNullException("watchedEventDelegate");
                }
            }

            /// <summary>
            /// Gets the unique id of this watcher.
            /// </summary>
            public ulong Id
            {
                get { return 0; }
            }

            /// <summary>
            /// Gets a value indicating whether the watcher is for a single use only.
            /// </summary>
            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            /// <summary>
            /// Gets or sets the kind of the watcher, if it is for single use and if the data is included on notification
            /// </summary>
            public WatcherKind Kind { get; set; }

            /// <summary>
            /// Invoked when a watched event occurs.
            /// </summary>
            /// <param name="watchedEvent">The event that occurred</param>
            public void Process(WatchedEvent watchedEvent)
            {
                this.watchedEventDelegate(watchedEvent);
            }
        }
    }
}
