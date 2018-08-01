// <copyright file="VegaFailoverCorrectness.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Networking.Infrastructure.RingMaster;
    using Azure.Networking.Infrastructure.RingMaster.Data;
    using VisualStudio.TestTools.UnitTesting;

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
        private static string serverAddress = "127.0.0.1:99";

        /// <summary>
        /// Request timeout to the backend
        /// </summary>
        private static int requestTimeout = 100000;

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

            if (context.Properties.ContainsKey("ServerAddress"))
            {
                serverAddress = context.Properties["ServerAddress"] as string;
            }
        }

        /// <summary>
        /// Creates a large VNET tree, with children adding and deleting continuously. Then checks if Stat.NumChildren
        /// and actual children count is reasonable.
        /// </summary>
        [TestMethod]
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
            new VegaFailoverCorrectness().TestNumChildrenFailover();
        }

        /// <summary>
        /// Creates the VNET base data before multi-thread operation is started.
        /// </summary>
        private static async Task CreateBaseData()
        {
            var client = new RingMasterClient(
                connectionString: serverAddress,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null);

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
            RingMasterClient client = null;
            var createMode = CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (client == null)
                {
                    client = new RingMasterClient(
                        connectionString: serverAddress,
                        clientCerts: null,
                        serverCerts: null,
                        requestTimeout: requestTimeout,
                        watcher: null);
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
