// <copyright file="TestRingMasterRing.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.EndToEndTests
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that validate that a ring manager ring works as expected.
    /// </summary>
    /// <remarks>
    /// Each of these tests creates a RingMaster ring with 3 instances where instance 0 is the primary
    /// that replicates to the other 2.
    /// With the above setup, various actions are performed and at the end of those actions the following validations are
    /// performed to verify that the RingMaster ring worked as expected.
    /// 1. The RingMaster snapshots produced by each ringmaster instance in each ringmaster ring have exactly the same nodes.
    /// 2. The Trees in all ringmaster instances are the same.
    /// </remarks>
    [TestClass]
    public sealed class TestRingMasterRing
    {
        private static Action<string> log = Console.WriteLine;

        [ClassInitialize]
        public static void TestRingMasterRingSetup(TestContext context)
        {
            log = s => context.WriteLine(s);

            LogFileEventTracing.Start(Path.Combine(Environment.CurrentDirectory, "TestLogs"));
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Backend-RingMasterEvents", EventLevel.Warning, "RingMasterBackendCore");
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence", EventLevel.Informational, "Persistence");
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence-InMemory", EventLevel.Informational, "InMemoryPersistence");
            Trace.Listeners.Add(new LogFileTraceListener());

            AppDomain.CurrentDomain.ProcessExit +=
                (sender, eventArgs) =>
                {
                    LogFileEventTracing.Stop();
                };
        }

        /// <summary>
        /// Verify the state of the ring after creating a deep hierarchy of nodes.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateDeepHierarchy()
        {
            VerifyRingMasterRingStateAfterAction((ringMaster, rootPath) =>
            {
                const int TotalNodeCount = 10000;

                // Create a hierarchy of 'NodesToBeCreated' nodes under the root path.
                using (var creator = new CreatePerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    creator.MinChildrenCountPerNode = 1;
                    creator.MaxChildrenCountPerNode = 4;
                    creator.MinDataSizePerNode = 0;
                    creator.MaxDataSizePerNode = 8;
                    creator.CreateHierarchy(ringMaster, rootPath, batchLength: 4, maxNodes: TotalNodeCount);
                }

                // Return the number of nodes created for verification
                return TotalNodeCount;
            }).Wait();
        }

        /// <summary>
        /// Verify the state of the ring after creating a set of nodes with random large data sizes.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateNodesWithLargeDataSizes()
        {
            VerifyRingMasterRingStateAfterAction((ringMaster, rootPath) =>
            {
                const int TotalNodeCount = 100;

                // Create a hierarchy of 'NodesToBeCreated' nodes under the root path.
                using (var creator = new CreatePerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    creator.MinChildrenCountPerNode = 1;
                    creator.MaxChildrenCountPerNode = 32;
                    creator.MinDataSizePerNode = 65536;
                    creator.MaxDataSizePerNode = 1024 * 1024;
                    creator.MinNodeNameLength = 1;
                    creator.MaxNodeNameLength = 1024;
                    creator.CreateHierarchy(ringMaster, rootPath, batchLength: 4, maxNodes: TotalNodeCount);
                }

                // Return the number of nodes created for verification
                return TotalNodeCount;
            }).Wait();
        }

        /// <summary>
        /// Verify the state of the ring after creating a node with a number of children that falls within the range
        /// where ringmaster would use a regular dictionary.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateFlat_RegularDictionary()
        {
            VerifyRingMasterRingStateAfterAction((ringMaster, rootPath) =>
            {
                int totalNodeCount = RingMasterInstance.MinSortedDictionaryThreshold - 1;

                // Create a hierarchy of 'NodesToBeCreated' nodes under the root path.
                using (var creator = new CreatePerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    creator.MinDataSizePerNode = 0;
                    creator.MaxDataSizePerNode = 8;
                    creator.MinNodeNameLength = 1;
                    creator.MaxNodeNameLength = 1024;
                    creator.CreateFlat(ringMaster, rootPath, batchLength: 4, maxNodes: totalNodeCount);
                }

                // Return the number of nodes created for verification
                return totalNodeCount;
            }).Wait();
        }

        /// <summary>
        /// Verify the state of the ring after creating a node with a number of children that falls within the
        /// range where ringmaster would use a sorted name value dictionary.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateFlat_SortedNameValueDictionary()
        {
            VerifyRingMasterRingStateAfterAction((ringMaster, rootPath) =>
            {
                int totalNodeCount = RingMasterInstance.MaxSortedDictionaryThreshold + 1;

                // Create a hierarchy of 'NodesToBeCreated' nodes under the root path.
                using (var creator = new CreatePerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    creator.MinDataSizePerNode = 0;
                    creator.MaxDataSizePerNode = 8;
                    creator.MinNodeNameLength = 1;
                    creator.MaxNodeNameLength = 1024;
                    creator.CreateFlat(ringMaster, rootPath, batchLength: 4, maxNodes: totalNodeCount);
                }

                // Return the number of nodes created for verification
                return totalNodeCount;
            }).Wait();
        }

        /// <summary>
        /// Verify the state of the ring after creating a hierarchy of nodes and deleting them individually.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteHierarchy()
        {
            VerifyRingMasterRingStateAfterAction((ringMaster, rootPath) =>
            {
                const int TotalNodeCount = 1000;

                // Create a hierarchy of 'NodesToBeCreated' nodes under the root path.
                using (var creator = new CreatePerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    creator.MinChildrenCountPerNode = 1;
                    creator.MaxChildrenCountPerNode = 4;
                    creator.MinDataSizePerNode = 0;
                    creator.MaxDataSizePerNode = 8;
                    creator.MinNodeNameLength = 1;
                    creator.MaxNodeNameLength = 1024;
                    creator.CreateHierarchy(ringMaster, rootPath, batchLength: 4, maxNodes: TotalNodeCount);
                }

                using (var deleter = new DeletePerformance(instrumentation: null, maxConcurrentRequests: 8, cancellationToken: CancellationToken.None))
                {
                    deleter.LoadNodes(ringMaster, rootPath, TotalNodeCount, maxGetChildrenEnumerationCount: 100).Wait();
                    deleter.QueueDeletes(ringMaster, batchLength: 8);
                }

                // Return the number of nodes created for verification
                return 0;
            }).Wait();
        }

        /// <summary>
        /// Verify the state of the ring after creating a hierarchy of nodes and deleting them all with one cascade delete operation.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCascadeDeleteHierarchy()
        {
            VerifyRingMasterRingStateAfterAction((ringMaster, rootPath) =>
            {
                const int TotalNodeCount = 1000;

                // Create a hierarchy of 'NodesToBeCreated' nodes under the root path.
                using (var creator = new CreatePerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    creator.MinChildrenCountPerNode = 1;
                    creator.MaxChildrenCountPerNode = 4;
                    creator.MinDataSizePerNode = 0;
                    creator.MaxDataSizePerNode = 8;
                    creator.MinNodeNameLength = 1;
                    creator.MaxNodeNameLength = 1024;
                    creator.CreateHierarchy(ringMaster, rootPath, batchLength: 4, maxNodes: TotalNodeCount);
                }

                using (var deleter = new DeletePerformance(instrumentation: null, maxConcurrentRequests: 8, cancellationToken: CancellationToken.None))
                {
                    deleter.CascadeDelete(ringMaster, rootPath);
                }

                // Return the number of nodes created for verification
                return 0;
            }).Wait();
        }

        /// <summary>
        /// Verify the state of the ring after creating a hierarchy of nodes and modifying the data of random nodes.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestModifyData()
        {
            VerifyRingMasterRingStateAfterAction((ringMaster, rootPath) =>
            {
                const int TotalNodeCount = 1000;

                // Create a hierarchy of 'NodesToBeCreated' nodes under the root path.
                using (var creator = new CreatePerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    creator.CreateHierarchy(ringMaster, rootPath, batchLength: 4, maxNodes: TotalNodeCount);
                }

                using (var setter = new SetDataPerformance(instrumentation: null, maxConcurrentRequests: 32, cancellationToken: CancellationToken.None))
                {
                    setter.LoadNodes(ringMaster, rootPath, TotalNodeCount).Wait();
                    setter.QueueRequests(ringMaster, batchLength: 8, maxOperations: 1000);
                }

                // Return the number of nodes created for verification
                return TotalNodeCount;
            }).Wait();
        }

        /// <summary>
        /// Verifies the transaction manager ring state after performing the given action.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private static async Task VerifyRingMasterRingStateAfterAction(Func<IRingMasterRequestHandler, string, int> action)
        {
            var clock = Stopwatch.StartNew();

            // Create a ringmaster ring with 3 members
            var ring = new RingMasterRing(1, memberCount: 3);
            await ring.Start();

            // Invoke the action with a connection to the primary transaction manager of the ring
            // the action will return the number of nodes that was created.
            int expectedNodes = 0;
            string rootPath = "/TestRoot";
            using (var ringMaster = ring.Connect())
            {
                await ringMaster.Create(rootPath, null, null, CreateMode.Persistent);

                log($"{clock.Elapsed} - Starting action");
                expectedNodes = action(ringMaster, rootPath);
                log($"{clock.Elapsed} - action finished");

                // Recreate the root path in case it was deleted by the action.
                await ringMaster.Create(rootPath, null, null, CreateMode.Persistent | CreateMode.SuccessEvenIfNodeExistsFlag);
            }

            // Verify that each ringmaster instance in each ringmaster ring creates a snapshot that contains exactly the
            // same nodes under the root path and which contains the same data as the tree in the primary.
            await ring.VerifyRingMasterDataConsistency(rootPath, expectedNodes + 1);
            log($"{clock.Elapsed} - data consistency verified");

            // Verify that the snapshot produced by each ringmaster instance is identical.
            ring.VerifyRingMasterSnapshots();
            log($"{clock.Elapsed} - snapshots verified");

            // Verify that each ringmaster instance applied the same change lists in the same order
            ring.VerifyCommittedChangeLists();
            log($"{clock.Elapsed} - committed change lists verified");
        }
    }
}
