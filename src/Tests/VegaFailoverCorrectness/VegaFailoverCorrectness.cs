// <copyright file="VegaFailoverCorrectness.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.Vega.Test.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The Vega Service Fabric failover correctness test intended running on local service fabric cluster.
    /// </summary>
    [TestClass]
    public class VegaFailoverCorrectness
    {
        /// <summary>
        /// Number of vnet nodes, equivalent of "/MadariUserData/vnets-XXX"
        /// </summary>
        private const int VnetCount = 1000;

        /// <summary>
        /// Number of children node in a VNET, equivalent of "/mappings/lnms"
        /// </summary>
        private const int ChildrenCount = 10;

        /// <summary>
        /// Minimal number of nodes in a VNET which will not be removed.
        /// </summary>
        private const int MinNodeCount = 2;

        /// <summary>
        /// Logging delegate
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Server endpoint
        /// </summary>
        private static string serverAddress;

        /// <summary>
        /// Request timeout to the backend
        /// </summary>
        private static int requestTimeout = 10000;

        /// <summary>
        /// Only run the test case for 60 minutes.
        /// </summary>
        private static int executionInMinutes = 60;

        /// <summary>
        /// Total number of operations being processed
        /// </summary>
        private static long totalOperationCount = 0;

        /// <summary>
        /// Total number of failures during the test.
        /// </summary>
        private static int totalFailures = 0;

        /// <summary>
        /// Precise stopwatch for measuring how long the primary was down
        /// </summary>
        private static Stopwatch clock = Stopwatch.StartNew();

        /// <summary>
        /// Class level setup
        /// </summary>
        /// <param name="context">Test context</param>
        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            log = s => context.WriteLine($"{DateTime.Now} {s}");

            if (context.Properties.Contains("ServerAddress"))
            {
                serverAddress = context.Properties["ServerAddress"] as string;
            }
            else
            {
                var serviceInfo = Helpers.Helpers.GetVegaServiceInfo().Result;
                serverAddress = serviceInfo.Item1;
            }
        }

        /// <summary>
        /// Tests the data consistency after failover.
        /// </summary>
        // [TestMethod] // Flaky
        public void TestDataConsistentAfterFailover()
        {
            const int MaxIndex = 100;

            // Running the test for 30 minutes. Even though it sounds long, using a shorter period makes the tests to fail almost all the time.
            TimeSpan testRunTime = TimeSpan.FromMinutes(30);
            string rootNode = Guid.NewGuid().ToString();
            TestDataConsistentAfterFailoverAsync().GetAwaiter().GetResult();

            async Task TestDataConsistentAfterFailoverAsync()
            {
                using (var client = new RetriableRingMasterClient(
                    s => new RingMasterClient(
                    connectionString: s,
                    clientCerts: null,
                    serverCerts: null,
                    requestTimeout: requestTimeout,
                    watcher: null),
                    serverAddress))
                {
                    await CreateTestData(client);

                    var clock = Stopwatch.StartNew();
                    int round = 0;
                    while (clock.Elapsed < testRunTime)
                    {
                        log($"test round {round++} begin:");
                        try
                        {
                            var initialRoot = await client.GetFullSubtree($"/{rootNode}", RequestGetSubtree.GetSubtreeOptions.None);
                            var initialStats = await GetAllStats(initialRoot, client);

                            TryKillPrimaryProcess();
                            await Task.Delay(30 * 1000);

                            var newRoot = await client.GetFullSubtree($"/{rootNode}", RequestGetSubtree.GetSubtreeOptions.None);
                            var newStats = await GetAllStats(newRoot, client);
                            log($"Get all nodes returned. Root: {newRoot.Name}. Number of nodes in current tree: {newStats.Count}");

                            Assert.IsTrue(CompareAllNodes(initialRoot, newRoot));
                            Assert.IsTrue(CompareAllStats(initialStats, newStats));

                            await UpdateSomeNodes(client);
                        }
                        catch (RingMasterException ex)
                        {
                            if (ex.ErrorCode != RingMasterException.Code.Operationtimeout)
                            {
                                throw;
                            }
                            else
                            {
                                log("operation timeout");
                                await Task.Delay(30 * 1000);
                            }
                        }
                    }
                }
            }

            async Task CreateTestData(IRingMasterRequestHandler client)
            {
                log("Begin creating test data " + serverAddress);

                var tasks = Enumerable.Range(0, MaxIndex)
                        .Select(index =>
                        {
                            var path = $"/{rootNode}/{index}/mappings/v4ca";
                            var data = Encoding.ASCII.GetBytes(path);
                            return client.Create(path, data, null, CreateMode.PersistentAllowPathCreation);
                        }).ToArray();

                await Task.WhenAll(tasks);

                var childTasks = new List<Task>();
                for (int index = 0; index < MaxIndex; index++)
                {
                    var ops = new List<Op>();
                    for (int i = 0; i < index; i++)
                    {
                        var childPath = $"/{rootNode}/{index}/mappings/v4ca/{i}";
                        var childData = Encoding.ASCII.GetBytes(childPath);
                        ops.Add(Op.Create(childPath, childData, null, CreateMode.Persistent));
                    }

                    childTasks.Add(client.Multi(ops));
                }

                await Task.WhenAll(childTasks);
                log($"Test data created at {rootNode}!");
            }

            async Task<int> UpdateSomeNodes(IRingMasterRequestHandler client)
            {
                var rnd = new Random();
                int count = rnd.Next(50);

                log($"Updating and Creating {count} nodes");
                for (int i = 0; i < count; i++)
                {
                    var index = rnd.Next(1, MaxIndex);
                    await client.SetData($"/{rootNode}/{index}/mappings/v4ca/{index - 1}", Helpers.Helpers.MakeRandomData(rnd, rnd.Next(128)), -1);
                    await client.Create($"/{rootNode}/{index}/mappings/v4ca/{Guid.NewGuid()}", Helpers.Helpers.MakeRandomData(rnd, rnd.Next(128)), null, CreateMode.Persistent);
                }

                return count;
            }

            bool CompareAllStats(Dictionary<string, Stat> stats1, Dictionary<string, Stat> stats2)
            {
                if (stats1.Count != stats2.Count)
                {
                    return false;
                }

                foreach (var pair in stats1)
                {
                    if (!stats2.ContainsKey(pair.Key) || !stats2[pair.Key].Equals(pair.Value))
                    {
                        log($"stats different at path {pair.Key}: stat1: {pair.Value} stat2: {stats2[pair.Key]}");
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Tests the user metadata consistent after failover.
        /// </summary>
        /// <returns>async task</returns>
        // [TestMethod]
        public async Task TestUserMetadataConsistentAfterFailover()
        {
            string path = $"/{Guid.NewGuid().ToString()}";
            int count = 100;
            var nodeData = Guid.NewGuid().ToByteArray();
            var nodeMetadata = Guid.NewGuid().ToByteArray();

            using (var client = new RetriableRingMasterClient(
                    s => new RingMasterClient(
                    connectionString: s,
                    clientCerts: null,
                    serverCerts: null,
                    requestTimeout: 2000,
                    watcher: null),
                    serverAddress))
            {
                for (int i = 0; i < count; i++)
                {
                    await client.Create($"{path}/{i}", nodeData, null, CreateMode.PersistentAllowPathCreation, nodeMetadata);
                }

                for (int round = 1; round <= 10; round++)
                {
                    log($"Test round {round}, killing process");
                    TryKillPrimaryProcess();
                    await Task.Delay(TimeSpan.FromMinutes(1));

                    log("Get data");
                    for (int i = 0; i < count; i++)
                    {
                        var data = await client.GetData($"{path}/{i}", RequestGetData.GetDataOptions.UserMetadataRequired, null);
                        Assert.IsTrue(data.Data.SequenceEqual(nodeData));
                        Assert.IsTrue(data.UserMetadata.SequenceEqual(nodeMetadata));
                        Assert.AreEqual(round, data.Stat.Version, data.Stat.Uversion);
                        if (i == 0)
                        {
                            log(data.Stat.ToString());
                        }

                        await client.SetDataAndUserMetadata($"{path}/{i}", nodeData, -1, nodeMetadata, -1);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a large VNET tree, with children adding and deleting continuously. Then checks if Stat.NumChildren
        /// and actual children count is reasonable.
        /// </summary>
        // [TestMethod] // Flaky
        public void TestNumChildrenFailover()
        {
            TestNumChildrenFailoverAsync().GetAwaiter().GetResult();
            Assert.AreEqual(0, totalFailures);

            // TAEF does not support async test case.
            async Task TestNumChildrenFailoverAsync()
            {
                using (var cancellation = new CancellationTokenSource())
                {
                    // Clean up the existing data and create the minimal number of children before adding/deleting starts.
                    await CreateBaseData();

                    var createNodeTask = Enumerable.Range(0, Environment.ProcessorCount)
                        .Select(n => Task.Run(() => CheckNodeThread(n, cancellation.Token)))
                        .ToList();

                    var lastCount = -1L;
                    while (clock.Elapsed.TotalMinutes < executionInMinutes)
                    {
                        await Task.Delay(30 * 1000);

                        var delta = lastCount > 0 ? totalOperationCount - lastCount : 0;
                        lastCount = totalOperationCount;

                        log($"Count={totalOperationCount} +{delta} Failures={totalFailures}");

                        try
                        {
                            TryKillPrimaryProcess();
                        }
                        catch (Exception ex)
                        {
                            log($"Exception in kill RM: {ex.Message}");
                        }
                    }

                    cancellation.Cancel();
                    await Task.WhenAll(createNodeTask);
                }
            }
        }

        private static void Main()
        {
            log = s => Console.WriteLine($"{DateTime.Now} {s}");
            log($"Started");
            var instance = new VegaFailoverCorrectness();
            log($"Finished. " + instance.ToString());

            // instance.TestUserMetadataConsistentAfterFailover().GetAwaiter().GetResult();
            // instance.TestDataConsistentAfterFailover();
            // instance.TestNumChildrenFailover();
        }

        private static async Task<Dictionary<string, Stat>> GetAllStats(TreeNode root, IRingMasterRequestHandler client)
        {
            var allStats = new Dictionary<string, Stat>();
            await GetStat(root, new StringBuilder());
            return allStats;

            async Task GetStat(TreeNode rootNode, StringBuilder sb)
            {
                if (rootNode == null)
                {
                    return;
                }

                int len = sb.Length;
                sb.Append($"/{rootNode.Name}");
                var path = sb.ToString();

                var stat = (Stat)await client.Exists(path, null);
                allStats.Add(path, stat);

                if (rootNode.Children != null)
                {
                    foreach (var child in rootNode.Children)
                    {
                        await GetStat(child, sb);
                    }
                }

                sb.Remove(len, rootNode.Name.Length + 1);
            }
        }

        private static bool CompareAllNodes(TreeNode root1, TreeNode root2)
        {
            // log($"comparing node {root1.Name} and {root2.Name}");
            if (root1 == null && root2 == null)
            {
                return true;
            }

            if (root1 == null || root2 == null)
            {
                return false;
            }

            if ((root1.Children != null && root1.Children.Count != root2.Children.Count)
                || root1.Name != root2.Name
                || (root1.Data != null && !root1.Data.SequenceEqual(root2.Data)))
            {
                log($"Node {root1.Name} from tree1 and node {root2.Name} from tree2 are different!");
                return false;
            }

            if (root1.Children != null)
            {
                foreach (var child in root1.Children)
                {
                    var root2Child = root2.Children.Where(n => n.Name == child.Name).FirstOrDefault();
                    if (!CompareAllNodes(child, root2Child))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void TryKillPrimaryProcess()
        {
            // Kill the primary if there are 5 replicas.  To be simple (and not have dependency on
            // Service Fabric), primary is assumed to be the process with the most number of handles
            // (which includes file handles and sockets).
            // Note this logic has to change for dotnet core.
            var processes = Process.GetProcessesByName("Microsoft.RingMaster.RingMasterService");
            if (processes.Length >= 5)
            {
                processes.OrderByDescending(p => p.HandleCount).First().Kill();
                log($"Killed a replica");
            }
            else
            {
                log($"Cannot kill process because only {processes.Length} are running");
            }
        }

        /// <summary>
        /// Creates the VNET base data before multi-thread operation is started.
        /// </summary>
        private static async Task CreateBaseData()
        {
            var client = new RetriableRingMasterClient(
                    s => new RingMasterClient(
                    connectionString: s,
                    clientCerts: null,
                    serverCerts: null,
                    requestTimeout: requestTimeout,
                    watcher: null),
                    serverAddress);

            var createMode = CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag;

            for (int vnetId = 0; vnetId < VnetCount; vnetId++)
            {
                await client.Delete($"/vnets-{vnetId}", -1, true);
                await client.Multi(
                    Enumerable.Range(0, MinNodeCount)
                    .Select(n => Op.Create($"/vnets-{vnetId}/lnms/dn-{vnetId}-{n}", null, null, createMode))
                    .ToList(),
                    true);
            }
        }

        /// <summary>
        /// Work load for adding / deleting and checking the number of children in VNET nodes
        /// </summary>
        /// <param name="id">Task sequence number to avoid write conflict</param>
        /// <param name="cancellationToken">Cancellation token to stop the operation</param>
        /// <returns>Async task to indicate the completion of operation</returns>
        private static async Task CheckNodeThread(int id, CancellationToken cancellationToken)
        {
            var lastMzxids = new long[VnetCount];
            IRingMasterRequestHandler client = null;
            var createMode = CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (client == null)
                {
                    client = new RetriableRingMasterClient(
                    s => new RingMasterClient(
                    connectionString: s,
                    clientCerts: null,
                    serverCerts: null,
                    requestTimeout: requestTimeout,
                    watcher: null),
                    serverAddress);
                }

                for (int vnetId = 0; vnetId < VnetCount; vnetId++)
                {
                    try
                    {
                        var parent = $"/vnets-{vnetId}/lnms";

                        // Create some children
                        await client.Multi(
                            Enumerable.Range(0, ChildrenCount).Select(n => Op.Create($"{parent}/node-{id}-{n}", null, null, createMode)).ToList(),
                            true);

                        var result = await client.Multi(new Op[] { Op.Check(parent, -1), Op.GetChildren(parent), }, true);

                        // Check number of children is correct -- it must be more than the number of children being created
                        var stat = ((OpResult.CheckResult)result[0]).Stat;
                        var children = ((OpResult.GetChildrenResult)result[1]).Children;

                        if (stat.NumChildren < MinNodeCount + ChildrenCount)
                        {
                            log($"Task {id}: wrong stat {stat.NumChildren} < {MinNodeCount + ChildrenCount}");
                            totalFailures++;
                        }

                        if (children.Count < MinNodeCount + ChildrenCount)
                        {
                            log($"Task {id}: wrong children {children.Count} < {MinNodeCount + ChildrenCount}");
                            totalFailures++;
                        }

                        if (stat.NumChildren != children.Count)
                        {
                            log($"Task {id}: stat {stat.NumChildren} inconsistent with children {children.Count}");
                            totalFailures++;
                        }

                        if (stat.NumChildren <= 0)
                        {
                            log($"Task {id}: Stat at {parent} is wrong: {stat}");
                            totalFailures++;
                        }

                        // Delete children being added -- the minimal number of children should be still there
                        await client.Multi(
                            Enumerable.Range(0, ChildrenCount).Select(n => Op.Delete($"{parent}/node-{id}-{n}", -1, false)).ToList(),
                            true);

                        result = await client.Multi(new Op[] { Op.Check(parent, -1), Op.GetChildren(parent), }, true);
                        stat = ((OpResult.CheckResult)result[0]).Stat;
                        children = ((OpResult.GetChildrenResult)result[1]).Children;

                        if (stat.NumChildren < MinNodeCount)
                        {
                            log($"Task {id}: wrong stat {stat.NumChildren} < {MinNodeCount}");
                            totalFailures++;
                        }

                        if (children.Count < MinNodeCount)
                        {
                            log($"Task {id}: wrong children {children.Count} < {MinNodeCount}");
                            totalFailures++;
                        }

                        if (stat.NumChildren != children.Count)
                        {
                            log($"Task {id}: stat {stat.NumChildren} inconsistent with children {children.Count}");
                            totalFailures++;
                        }

                        if (stat.NumChildren <= 0)
                        {
                            log($"Task {id}: Stat at {parent} is wrong: {stat}");
                            totalFailures++;
                        }

                        totalOperationCount++;
                    }
                    catch (Exception ex)
                    {
                        client = null;

                        log($"Task {id}: Exception: {ex.Message}");

                        break;
                    }
                }
            }
        }
    }
}
