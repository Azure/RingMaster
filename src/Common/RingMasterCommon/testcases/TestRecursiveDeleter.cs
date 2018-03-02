// <copyright file="TestRecursiveDeleter.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify recursive delete functionality.
    /// </summary>
    public sealed class TestRecursiveDeleter : RingMasterInterfaceTest
    {
        /// <summary>
        /// The prefix which this test will use to create nodes.
        /// </summary>
        private const string TestPrefix = "/$rmbvt/TestRecursiveDeleter";

        private const string AuthDigest1 = "b180ee5f-56ba-430e-a20d-ecaba4ab4fab";

        private const string AuthDigest2 = "48dd2f1d-f3ca-4d5e-903c-79f3f0b78bbe";

        private static readonly Id Digest1Identity = new Id(AuthSchemes.Digest, AuthDigest1);

        private static readonly Id Digest2Identity = new Id(AuthSchemes.Digest, AuthDigest2);

        private static readonly Acl AllowDigest1 = new Acl((int)Acl.Perm.ALL, Digest1Identity);

        private static readonly Acl AllowDigest1Read = new Acl((int)Acl.Perm.READ, Digest1Identity);

        private static readonly Acl AllowDigest2 = new Acl((int)Acl.Perm.ALL, Digest2Identity);

        /// <summary>
        /// Initializes the test.
        /// </summary>
        public void Initialize()
        {
            Task.Run(async () =>
                {
                    using (var ringMaster = this.ConnectToRingMaster())
                    {
                        Trace.TraceInformation("Initializing TestRoot {0}", TestRecursiveDeleter.TestPrefix);
                        try
                        {
                            await ringMaster.Create(TestRecursiveDeleter.TestPrefix, null, null, CreateMode.PersistentAllowPathCreation);
                        }
                        catch (RingMasterException ex)
                        {
                            Assert.AreEqual(RingMasterException.Code.Nodeexists, ex.ErrorCode);
                        }
                    }
                }).Wait();
        }

        /// <summary>
        /// Verify that recursively deleting a non existent node fails.
        /// </summary>
        public async Task TestDeleteNonExistentNode()
        {
            var instrumentation = new TestRecursiveDeleterInstrumentation();
            var deleter = new RecursiveDeleter(instrumentation);

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = $"{TestRecursiveDeleter.TestPrefix}/TestRecursiveDeleteNonExistentNode_{Guid.NewGuid()}";

                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () => await deleter.Delete(ringMaster, nodePath, CancellationToken.None),
                    $"Delete should not succeed for non-existent node {nodePath}");

                Assert.AreEqual(0, instrumentation.NodesDeletedCount);
            }
        }

        /// <summary>
        /// Verify that recursively deleting a node that has no children deletes the node.
        /// </summary>
        public async Task TestDeleteEmptyNode()
        {
            var instrumentation = new TestRecursiveDeleterInstrumentation();
            var deleter = new RecursiveDeleter(instrumentation, maxChildrenEnumerationCount: 10);

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = $"{TestRecursiveDeleter.TestPrefix}/TestRecursiveDeleteEmptyNode_{Guid.NewGuid()}";
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                int deletedCount = await deleter.Delete(ringMaster, nodePath, CancellationToken.None);

                await VerifyRingMasterException(RingMasterException.Code.Nonode, async () => await ringMaster.Exists(nodePath, watcher: null), $"{nodePath} should not exist");

                Assert.AreEqual(1, instrumentation.NodesDeletedCount);
                Assert.AreEqual(1, deletedCount);
            }
        }

        /// <summary>
        /// Verify that cancellation token is observed.
        /// </summary>
        public async Task TestCancelDelete()
        {
            var instrumentation = new TestRecursiveDeleterInstrumentation();
            var deleter = new RecursiveDeleter(instrumentation);

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = $"{TestRecursiveDeleter.TestPrefix}/TestCancelRecursiveDelete_{Guid.NewGuid()}";
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.Cancel();

                try
                {
                    Trace.TraceInformation($"Attempting to delete {nodePath}");
                    await deleter.Delete(ringMaster, nodePath, cancellationTokenSource.Token);
                    Assert.Fail("Delete should have thrown TaskCanceledException");
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation($"Exception: {ex}");
                }

                await ringMaster.Exists(nodePath, watcher: null);

                Assert.AreEqual(0, instrumentation.NodesDeletedCount);
                Assert.AreEqual(1, instrumentation.RecursiveDeleteFailedCount);
            }
        }

        /// <summary>
        /// Verify recursively deleting a node that has children.
        /// </summary>
        public async Task TestDeleteNodeWithChildren(int childrenCount = 50)
        {
            var instrumentation = new TestRecursiveDeleterInstrumentation();
            var deleter = new RecursiveDeleter(instrumentation, maxChildrenEnumerationCount: 10);

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = $"{TestRecursiveDeleter.TestPrefix}/TestRecursiveDeleteNodeWithChildren_{Guid.NewGuid()}";
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                for (int i = 0; i < childrenCount; i++)
                {
                    await ringMaster.Create($"{nodePath}/Child{i}", null, null, CreateMode.Persistent);
                }

                int deletedCount = await deleter.Delete(ringMaster, nodePath, CancellationToken.None);

                await VerifyRingMasterException(RingMasterException.Code.Nonode, async () => await ringMaster.Exists(nodePath, watcher: null), $"{nodePath} should not exist");

                Assert.AreEqual(childrenCount + 1, instrumentation.NodesDeletedCount);
                Assert.AreEqual(childrenCount + 1, deletedCount);
                Assert.AreEqual(childrenCount + 1, instrumentation.DeleteMultiSucceededOperationsCount);
                Assert.AreEqual(1, instrumentation.RecursiveDeleteSucceededCount);
                Assert.AreEqual(0, instrumentation.RecursiveDeleteFailedCount);
            }
        }

        /// <summary>
        /// Verify failure to delete a node is handled properly.
        /// </summary>
        public async Task TestDeleteNodeFailure(int childrenCount = 5)
        {
            var instrumentation = new TestRecursiveDeleterInstrumentation();
            var deleter = new RecursiveDeleter(instrumentation, maxChildrenEnumerationCount: 10);
            deleter.MaxDeleteBatchLength = 1;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = $"{TestRecursiveDeleter.TestPrefix}/TestRecursiveDeleteNodeFailure_{Guid.NewGuid()}";

                await ringMaster.SetAuth(Digest1Identity);

                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                await ringMaster.SetAuth(Digest2Identity);

                await ringMaster.Create($"{nodePath}/Child0", null, new Acl[] { AllowDigest2, AllowDigest1Read }, CreateMode.Persistent);

                await ringMaster.SetAuth(Digest1Identity);

                for (int i = 1; i < childrenCount; i++)
                {
                    await ringMaster.Create($"{nodePath}/Child{i}", null, new Acl[] { AllowDigest1 }, CreateMode.Persistent);
                }

                // We should be able to delete all the child nodes,
                // but not the parent node.
                await VerifyRingMasterException(RingMasterException.Code.Authfailed, async () => await deleter.Delete(ringMaster, nodePath, CancellationToken.None), $"{nodePath} should not be deleted");

                Assert.AreEqual(childrenCount - 1, instrumentation.NodesDeletedCount);
                Assert.AreEqual(childrenCount - 1, instrumentation.DeleteMultiSucceededOperationsCount);
                Assert.AreEqual(1, instrumentation.DeleteMultiFailedOperationsCount);
                Assert.AreEqual(0, instrumentation.RecursiveDeleteSucceededCount);
                Assert.AreEqual(1, instrumentation.RecursiveDeleteFailedCount);
            }
        }

        /// <summary>
        /// Verify recursively deleting a node that has tree of children.
        /// </summary>
        public async Task TestDeleteNodeWithChildTree(int childrenCount = 1000, int childrenPerLevel = 10)
        {
            var instrumentation = new TestRecursiveDeleterInstrumentation();
            var deleter = new RecursiveDeleter(instrumentation, maxChildrenEnumerationCount: 10);

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = $"{TestRecursiveDeleter.TestPrefix}/TestRecursiveDeleteNodeWithChildTree_{Guid.NewGuid()}";
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                var nodeStack = new Stack<string>();

                nodeStack.Push(nodePath);

                int childrenCreated = 0;

                while ((nodeStack.Count > 0) && (childrenCreated < childrenCount))
                {
                    string path = nodeStack.Pop();
                    for (int i = 0; i < childrenPerLevel; i++)
                    {
                        string childPath = $"{path}/Child{i}";
                        await ringMaster.Create(childPath, null, null, CreateMode.Persistent);
                        childrenCreated++;
                        nodeStack.Push(childPath);
                    }
                }

                int deletedCount = await deleter.Delete(ringMaster, nodePath, CancellationToken.None);

                await VerifyRingMasterException(RingMasterException.Code.Nonode, async () => await ringMaster.Exists(nodePath, watcher: null), $"{nodePath} should not exist");

                Assert.AreEqual(childrenCount + 1, instrumentation.NodesDeletedCount);
                Assert.AreEqual(childrenCount + 1, deletedCount);
                Assert.AreEqual(childrenCount + 1, instrumentation.DeleteMultiSucceededOperationsCount);
                Assert.AreEqual(1, instrumentation.RecursiveDeleteSucceededCount);
                Assert.AreEqual(0, instrumentation.RecursiveDeleteFailedCount);
            }
        }

        /// <summary>
        /// Verify recursively deleting a node that has a lot of immediate children
        /// </summary>
        /// <param name="childrenCount">Number of children to create</param>
        public async Task TestDeleteNodeWithManyChildren(int childrenCount = 10000)
        {
            var instrumentation = new TestRecursiveDeleterInstrumentation();
            var deleter = new RecursiveDeleter(instrumentation, maxChildrenEnumerationCount: 10);

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodePath = $"{TestRecursiveDeleter.TestPrefix}/TestRecursiveDeleteNodeWithManyChildren_{Guid.NewGuid()}";
                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                for (int i = 0; i < childrenCount; i++)
                {
                    string childPath = $"{nodePath}/Child{i}";
                    await ringMaster.Create(childPath, null, null, CreateMode.Persistent);
                }

                int deletedCount = await deleter.Delete(ringMaster, nodePath, CancellationToken.None);

                await VerifyRingMasterException(RingMasterException.Code.Nonode, async () => await ringMaster.Exists(nodePath, watcher: null), $"{nodePath} should not exist");

                Assert.AreEqual(childrenCount + 1, instrumentation.NodesDeletedCount);
                Assert.AreEqual(childrenCount + 1, deletedCount);
                Assert.AreEqual(childrenCount + 1, instrumentation.DeleteMultiSucceededOperationsCount);
                Assert.AreEqual(1, instrumentation.RecursiveDeleteSucceededCount);
                Assert.AreEqual(0, instrumentation.RecursiveDeleteFailedCount);
            }
        }

        /// <summary>
        /// Test Instrumentation consumer for the <see cref="RecursiveDeleter"/> class.
        /// </summary>
        private sealed class TestRecursiveDeleterInstrumentation : RecursiveDeleter.IInstrumentation
        {
            /// <summary>
            /// Gets the number of nodes that were successfully deleted.
            /// </summary>
            public int NodesDeletedCount { get; private set; }

            /// <summary>
            /// Gets the number of times recursive delete succeeded.
            /// </summary>
            public int RecursiveDeleteSucceededCount { get; private set; }

            /// <summary>
            /// Gets the number of times recursive delete failed.
            /// </summary>
            public int RecursiveDeleteFailedCount { get; private set; }

            /// <summary>
            /// Gets the number of times delete operations succeeded.
            /// </summary>
            public int DeleteMultiSucceededOperationsCount { get; private set; }

            /// <summary>
            /// Gets the number of times delete operations failed.
            /// </summary>
            public int DeleteMultiFailedOperationsCount { get; private set; }

            /// <inheritdoc />
            public void DeleteMultiSucceeded(int operationsCount, TimeSpan latency)
            {
                Trace.TraceInformation($"DeleteMultiSucceeded operationsCount={operationsCount}");
                lock (this)
                {
                    this.DeleteMultiSucceededOperationsCount += operationsCount;
                }
            }

            /// <inheritdoc />
            public void DeleteMultiFailed(int operationsCount, TimeSpan latency)
            {
                Trace.TraceInformation($"DeleteMultiFailed operationsCount={operationsCount}");
                lock (this)
                {
                    this.DeleteMultiFailedOperationsCount += operationsCount;
                }
            }

            /// <inheritdoc />
            public void RecursiveDeleteSucceeded(int nodesDeleted, TimeSpan latency)
            {
                Trace.TraceInformation($"RecursiveDeleteSucceeded nodesDeleted={nodesDeleted}");
                lock (this)
                {
                    this.NodesDeletedCount += nodesDeleted;
                    this.RecursiveDeleteSucceededCount++;
                }
            }

            /// <inheritdoc />
            public void RecursiveDeleteFailed(int nodesDeleted, TimeSpan latency)
            {
                Trace.TraceInformation($"RecursiveDeleteFailed nodesDeleted={nodesDeleted}");
                lock (this)
                {
                    // Even if RecursiveDelete fails some nodes could have been successfully deleted.
                    this.NodesDeletedCount += nodesDeleted;
                    this.RecursiveDeleteFailedCount++;
                }
            }
        }
    }
}
