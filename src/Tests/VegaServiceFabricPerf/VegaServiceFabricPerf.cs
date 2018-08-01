// <copyright file="VegaServiceFabricPerf.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Networking.Infrastructure.RingMaster;
    using Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Vega.Test.Helpers;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The Vega Service Fabric Performance test class
    /// </summary>
    [TestClass]
    public class VegaServiceFabricPerf
    {
        private const int MaxChildrenCount = 256;
        private const int PrintFailureThreshold = 100;

        private static IConfiguration appSettings;

        /// <summary>
        /// Logging delegate
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Endpoint of the backend
        /// </summary>
        private static string server;

        /// <summary>
        /// Name of the root node. If multiple instances of perf test are running, each one can choose a different
        /// name to stress the service in parallel.
        /// </summary>
        private static string rootNodeName;

        /// <summary>
        /// RingMaster clients, i.e. connections to the backend
        /// </summary>
        private static IRingMasterRequestHandler[] clients;

        /// <summary>
        /// Minimum data payload size
        /// </summary>
        private static int minDataSize = 256;

        /// <summary>
        /// Maximum data payload size
        /// </summary>
        private static int maxDataSize = 16384;

        /// <summary>
        /// Request timeout to the backend
        /// </summary>
        private static int requestTimeout = 100000;

        /// <summary>
        /// Number of thread to send request in parallel
        /// </summary>
        private static int threadCount = -1;

        /// <summary>
        /// Number of seconds each test should run
        /// </summary>
        private static int testCaseSeconds = 100;

        /// <summary>
        /// Number of batched operation in a group
        /// </summary>
        private static int batchOpCount = 32;

        /// <summary>
        /// Number of async task to await in a batch
        /// </summary>
        private static int asyncTaskCount = 64;

        /// <summary>
        /// Total amount of data being processed
        /// </summary>
        private static long totalDataSize = 0;

        /// <summary>
        /// Total number of data items being processed
        /// </summary>
        private static long totalDataCount = 0;

        /// <summary>
        /// Total number of failures in each test
        /// </summary>
        private static long totalFailures = 0;

        /// <summary>
        /// The large tree roots
        /// </summary>
        private static List<Guid> largeTreeRoots;

        /// <summary>
        /// The create test will create a large number of small trees (child number 0 - 20)
        /// and a small number (20 - 50) large trees, which have a lot more children.
        /// Each thread in the test switches between creating small trees and large trees continuously.
        /// So this magic number actually means, after creating one small tree,
        /// how many large tree nodes should it create.
        /// </summary>
        private static int largeTreeRatio = 50;

        /// <summary>
        /// The queue that used in the producer/consumer model,
        /// stores the nodes that have been traversed by the helper client
        /// and yet to be visit by the consumers (get/set/delete).
        /// </summary>
        private static ConcurrentQueue<string> queuedNodes;

        /// <summary>
        /// The helper client
        /// </summary>
        private static IRingMasterRequestHandler helperClient;

        /// <summary>
        /// Whether to cleanup test data in the Class cleanup method.
        /// </summary>
        private static bool cleanup = true;

        /// <summary>
        /// Test setup
        /// </summary>
        /// <param name="context">test context for logging</param>
        [ClassInitialize]
        public static void TestClassInitialize(TestContext context)
        {
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            appSettings = builder.Build();

            minDataSize = int.Parse(appSettings["MinDataSize"]);
            maxDataSize = int.Parse(appSettings["MaxDataSize"]);
            batchOpCount = int.Parse(appSettings["BatchOpCount"]);
            testCaseSeconds = int.Parse(appSettings["TestCaseSeconds"]);
            requestTimeout = int.Parse(appSettings["RequestTimeout"]);
            threadCount = int.Parse(appSettings["ThreadCount"]);
            asyncTaskCount = int.Parse(appSettings["AsyncTaskCount"]);
            largeTreeRatio = int.Parse(appSettings["LargeTreeRatio"]);
            cleanup = bool.Parse(appSettings["CleanupTestData"]);
            server = appSettings["ServerAddress"];
            rootNodeName = appSettings["RootNodeName"];

            Random rnd = new Random();

            // number of large trees will be a random number between (20, 50)
            largeTreeRoots = Enumerable.Range(0, rnd.Next(20, 50)).Select(x => Guid.NewGuid()).ToList();

            if (threadCount < 0)
            {
                threadCount = Environment.ProcessorCount;
            }

            if (context.GetType().Name.StartsWith("Dummy"))
            {
                log = s => context.WriteLine(s);
            }
            else
            {
                log = s => Trace.TraceInformation(s);
            }

            var serviceInfo = default(Tuple<string, string>);

            // Allow the endpoint address being specified from the command line
            if (string.IsNullOrEmpty(server))
            {
                // Only queries the service info if the service address is not specified, e.g. when running in CloudTest
                // on local machine.
                serviceInfo = Helpers.GetVegaServiceInfo().Result;
                server = serviceInfo.Item1;
            }

            clients = Enumerable.Range(0, threadCount).Select(x => new RetriableRingMasterClient(
                s => new RingMasterClient(
                connectionString: s,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null),
                server))
                .ToArray();

            helperClient = new RetriableRingMasterClient(
                s => new RingMasterClient(
                connectionString: s,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null),
                server);

            // Ensure the sufficient threads in the pool for the async tasks.
            ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
            ThreadPool.SetMinThreads(threadCount * 2, minIOC);

            queuedNodes = new ConcurrentQueue<string>();
            InitializeMdm(serviceInfo?.Item2, appSettings);
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        [ClassCleanup]
        public static void TestClassCleanup()
        {
            try
            {
                if (cleanup)
                {
                    log("cleaning up test data...");
                    CleanupTestData();
                }
            }
            catch
            {
                log("cleanup test data failed. The data may or may not been deleted");
            }
            finally
            {
                foreach (var client in clients)
                {
                    client.Dispose();
                }

                helperClient.Dispose();
            }
        }

        /// <summary>
        /// Initializes the MDM.
        /// </summary>
        /// <param name="roleInstance">The role instance.</param>
        /// <param name="appSettings">The app settings.</param>
        public static void InitializeMdm(string roleInstance, IConfiguration appSettings)
        {
            if (!bool.TryParse(appSettings["MdmEnabled"], out bool mdmEnabled))
            {
                mdmEnabled = false;
            }

            var environment = appSettings["Environment"];
            var tenant = appSettings["Tenant"];
            var mdmAccountName = appSettings["MdmAccountName"];

            MdmHelper.Initialize(environment, tenant, mdmAccountName, roleInstance, string.Empty, MdmConstants.VegaServiceFabricPerfIfxSession, MdmConstants.ServiceFabricPerfMdmNamespace, mdmEnabled);
        }

        /// <summary>
        /// Pings the service to measure the overhead of basic request processing path without any read/write operation
        /// </summary>
        [TestMethod]
        public void TestRingMasterPingPong()
        {
            ResetCounts();
            var rate = TestFlowAsync(
                "Ping-Pong Test",
                OperationType.PingPong,
                async (client, cancellationToken, threadId) =>
                {
                    var clock = Stopwatch.StartNew();
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var startTime = clock.Elapsed;
                            var tasks = Enumerable.Range(0, asyncTaskCount)
                                .Select(task => client.Exists(string.Empty, null, true)
                                        .ContinueWith(t =>
                                        {
                                            var duration = clock.Elapsed - startTime;
                                            MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.PingPong);
                                        }))
                                .ToArray();

                            await Task.WhenAll(tasks);

                            Interlocked.Add(ref totalDataCount, tasks.Length);
                        }
                        catch (Exception ex)
                        {
                            if (Interlocked.Increment(ref totalFailures) < PrintFailureThreshold)
                            {
                                log($"Failed to call Batch: {ex.Message}");
                            }
                        }
                    }
                },
                testCaseSeconds)
                .GetAwaiter().GetResult();
            log($"Ping-Pong test rate: {rate:G4} /sec");
        }

        /// <summary>
        /// Create data node performance
        /// </summary>
        [TestMethod]
        public void TestRingMasterCreate()
        {
            ResetCounts();

            var rate = TestFlowAsync(
                "Create data node perf test",
                OperationType.Create,
                CreateNodeThread,
                testCaseSeconds * 6)
                .GetAwaiter().GetResult();
            log($"Node create rate: {rate:G4} /sec");
        }

        /// <summary>
        /// Get sub-tree performance
        /// </summary>
        [TestMethod]
        public void TestRingMasterGetFullSubtree()
        {
            ResetCounts();
            queuedNodes = new ConcurrentQueue<string>();
            var cancellation = new CancellationTokenSource();

            Task.Run(async () =>
            {
                var startFrom = string.Empty;
                while (!cancellation.IsCancellationRequested)
                {
                    var children = await helperClient.GetChildren($"/{rootNodeName}", null, $">:{MaxChildrenCount}:{startFrom}");
                    foreach (var child in children)
                    {
                        queuedNodes.Enqueue($"/{rootNodeName}/{child}/mappings/v4ca");
                        startFrom = child;
                    }

                    if (children.Count < MaxChildrenCount)
                    {
                        break;
                    }
                }
            });

            var rate = TestFlowAsync(
                "Get full sub-tree perf test",
                OperationType.GetFullSubtree,
                GetFullSubtreeThread,
                testCaseSeconds * 4)
                .GetAwaiter().GetResult();

            cancellation.Cancel();
            log($"get full subtree rate: {rate:G4} /sec");
        }

        /// <summary>
        /// RingMaster get node test
        /// </summary>
        [TestMethod]
        public void TestRingMasterGet()
        {
            ResetCounts();
            queuedNodes = new ConcurrentQueue<string>();

            var cancellation = new CancellationTokenSource();
            Task.Run(() => TraverseTree(new List<string>() { rootNodeName }, cancellation.Token));
            Task.Run(() => CheckQueueCount(cancellation.Token));

            var rate = TestFlowAsync(
                "Get data node perf test",
                OperationType.Get,
                GetNodeThread,
                testCaseSeconds)
                .GetAwaiter().GetResult();

            cancellation.Cancel();

            log($"Node get rate: {rate:G4} /sec");
        }

        /// <summary>
        /// RingMaster get node test
        /// </summary>
        [TestMethod]
        public void TestRingMasterSet()
        {
            ResetCounts();
            queuedNodes = new ConcurrentQueue<string>();

            var cancellation = new CancellationTokenSource();
            Task.Run(() => TraverseTree(new List<string>() { rootNodeName }, cancellation.Token));
            Task.Run(() => CheckQueueCount(cancellation.Token));

            var rate = TestFlowAsync(
                "Set node perf test",
                OperationType.Set,
                SetNodeThread,
                testCaseSeconds * 2)
                .GetAwaiter().GetResult();

            cancellation.Cancel();

            log($"Node set rate: {rate:G4} /sec");
        }

        /// <summary>
        /// Test the batched creation
        /// </summary>
        [TestMethod]
        public void TestRingMasterBatchCreate()
        {
            ResetCounts();
            var rate = TestBatchOrMultiCreate(true, OperationType.BatchCreate).GetAwaiter().GetResult();
            log($"Node batch create rate: {rate:G4} /sec");
        }

        /// <summary>
        /// Test the multi creation
        /// </summary>
        [TestMethod]
        public void TestRingMasterMultiCreate()
        {
            ResetCounts();
            var rate = TestBatchOrMultiCreate(false, OperationType.MultiCreate).GetAwaiter().GetResult();
            log($"Node multi create rate: {rate:G4} /sec");
        }

        /// <summary>
        /// Tests the ring master delete.
        /// </summary>
        [TestMethod]
        public void TestRingMasterDelete()
        {
            ResetCounts();
            queuedNodes = new ConcurrentQueue<string>();

            var cancellation = new CancellationTokenSource();
            Task.Run(() => TraverseTree(new List<string>() { rootNodeName, $"{rootNodeName}_Batch", $"{rootNodeName}_Multi" }, cancellation.Token));
            Task.Run(() => CheckQueueCount(cancellation.Token));

            Thread.Sleep(2 * 1000);
            var rate = TestFlowAsync(
                "Delete node perf test",
                OperationType.Delete,
                (client, cancellationToken, threadId) =>
                {
                    int taskCount = 0;
                    var clock = Stopwatch.StartNew();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        while (queuedNodes.TryDequeue(out string path) && !cancellationToken.IsCancellationRequested)
                        {
                            SpinWait.SpinUntil(() => taskCount < asyncTaskCount || cancellationToken.IsCancellationRequested);
                            var startTime = clock.Elapsed;
                            var task = client.Delete(path, -1, DeleteMode.None)
                                    .ContinueWith(
                                        t =>
                                        {
                                            Interlocked.Decrement(ref taskCount);
                                            if (!t.Result)
                                            {
                                                log($"Failed to delete {path}.");
                                                if (t.Exception != null)
                                                {
                                                    if (Interlocked.Increment(ref totalFailures) < PrintFailureThreshold)
                                                    {
                                                        log($"Exception: {t.Exception.Message}");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Interlocked.Increment(ref totalDataCount);

                                                var duration = clock.Elapsed - startTime;
                                                MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Delete);
                                            }
                                        });

                            Interlocked.Increment(ref taskCount);
                        }
                    }

                    SpinWait.SpinUntil(() => taskCount == 0);

                    return Task.FromResult(0);
                },
                testCaseSeconds * 4)
                .GetAwaiter().GetResult();

            log($"Node delete rate: {rate:G4} /sec");
            cancellation.Cancel();
        }

        private static async Task TraverseTree(List<string> treeRoots, CancellationToken cancellation)
        {
            var allRoots = await helperClient.GetChildren("/", null);
            treeRoots.RemoveAll(r => !allRoots.Contains(r));

            int idx = 0;
            while (idx < treeRoots.Count && !cancellation.IsCancellationRequested)
            {
                var rootName = treeRoots[idx++];

                log("start traversing " + rootName);
                var startFrom = string.Empty;
                while (!cancellation.IsCancellationRequested)
                {
                    var children = await helperClient.GetChildren($"/{rootName}", null, $">:{MaxChildrenCount}:{startFrom}");

                    foreach (var child in children)
                    {
                        var subroot = $"/{rootName}/{child}/mappings/v4ca";
                        var subtreeStartFrom = string.Empty;
                        while (!cancellation.IsCancellationRequested)
                        {
                            IReadOnlyList<string> leafChildren = new List<string>();
                            try
                            {
                                leafChildren = await helperClient.GetChildren(subroot, null, $">:{MaxChildrenCount}:{subtreeStartFrom}");
                            }
                            catch (RingMasterException ex)
                            {
                                if (ex.ErrorCode != RingMasterException.Code.Nonode)
                                {
                                    throw;
                                }
                            }

                            foreach (var leaf in leafChildren)
                            {
                                queuedNodes.Enqueue($"{subroot}/{leaf}");
                                subtreeStartFrom = leaf;
                            }

                            if (leafChildren.Count < MaxChildrenCount)
                            {
                                break;
                            }
                        }

                        startFrom = child;
                    }

                    if (children.Count < MaxChildrenCount)
                    {
                        break;
                    }
                }

                log("finished traversing " + rootName);
            }
        }

        private static async Task CheckQueueCount(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                log($"Queue size: {queuedNodes.Count}");
            }
        }

        /// <summary>
        /// Tests create scenario using either batch or multi
        /// </summary>
        /// <param name="batch">true if using batch, false if using multi</param>
        /// <param name="operationType">Type of the operation.</param>
        /// <returns>
        /// Request per second
        /// </returns>
        private static async Task<double> TestBatchOrMultiCreate(bool batch, OperationType operationType)
        {
            var name = batch ? "Batch" : "Multi";
            return await TestFlowAsync(
                $"{name}(Create) node perf test",
                operationType,
                async (client, cancellationToken, threadId) =>
                {
                    var rnd = new Random();
                    var clock = Stopwatch.StartNew();
                    var rootName = $"{rootNodeName}_{name}";
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var ops = new List<Op>(batchOpCount);
                        var totalSize = 0;

                        int smallTreeNodeCount = rnd.Next(batchOpCount / 2);
                        int bigTreeNodeCount = batchOpCount - smallTreeNodeCount;

                        var smallSubtreeId = Guid.NewGuid();
                        while (smallTreeNodeCount-- > 0)
                        {
                            var data = Helpers.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));
                            totalSize += data.Length;
                            ops.Add(Op.Create($"/{rootName}/vnet{smallSubtreeId}/mappings/v4ca/{Guid.NewGuid()}", data, null, CreateMode.PersistentAllowPathCreation));
                        }

                        while (bigTreeNodeCount-- > 0)
                        {
                            var data = Helpers.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));
                            totalSize += data.Length;
                            int idx = rnd.Next(largeTreeRoots.Count);
                            ops.Add(Op.Create($"/{rootName}/vnet{largeTreeRoots[idx]}/mappings/v4ca/{Guid.NewGuid()}", data, null, CreateMode.PersistentAllowPathCreation));
                        }

                        try
                        {
                            var startTime = clock.Elapsed;
                            if (batch)
                            {
                                await client.Batch(ops).ConfigureAwait(false);
                            }
                            else
                            {
                                await client.Multi(ops).ConfigureAwait(false);
                            }

                            var duration = clock.Elapsed - startTime;
                            MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, operationType);

                            Interlocked.Add(ref totalDataSize, totalSize);
                            Interlocked.Add(ref totalDataCount, ops.Count);
                        }
                        catch (Exception ex)
                        {
                            if (Interlocked.Increment(ref totalFailures) < PrintFailureThreshold)
                            {
                                log($"Failed to call {name}: {ex.Message}");
                            }
                        }
                    }
                },
                testCaseSeconds);
        }

        /// <summary>
        /// Work load for testing SetNode method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static Task SetNodeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;
            var rnd = new Random();
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                while (queuedNodes.TryDequeue(out string path) && !token.IsCancellationRequested)
                {
                    var data = Helpers.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

                    SpinWait.SpinUntil(() => taskCount < asyncTaskCount || token.IsCancellationRequested);
                    var startTime = clock.Elapsed;
                    var task = client.SetData(path, data, -1)
                            .ContinueWith(
                            t =>
                            {
                                Interlocked.Decrement(ref taskCount);

                                if (t.Exception != null)
                                {
                                    if (Interlocked.Increment(ref totalFailures) < PrintFailureThreshold)
                                    {
                                        log($"Failed to set {path}: {t.Exception.Message}");
                                    }
                                }
                                else
                                {
                                    Interlocked.Add(ref totalDataSize, data.Length);
                                    Interlocked.Increment(ref totalDataCount);

                                    var duration = clock.Elapsed - startTime;
                                    MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Set);
                                }
                            });

                    Interlocked.Increment(ref taskCount);

                    queuedNodes.Enqueue(path);
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for testing Create method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static Task CreateNodeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            var rnd = new Random();
            int taskCount = 0;
            var clock = Stopwatch.StartNew();
            bool createSmallTree = true;

            while (!token.IsCancellationRequested)
            {
                var dataSize = 0;
                var dataCount = 0;
                int numToCreate = 0;
                var subtree = string.Empty;

                if (createSmallTree)
                {
                    numToCreate = rnd.Next(0, 20);
                    subtree = $"/{rootNodeName}/vnet{Guid.NewGuid()}/mappings/v4ca";
                }
                else
                {
                    // create big tree children;
                    numToCreate = largeTreeRatio;
                    int idx = rnd.Next(largeTreeRoots.Count);
                    subtree = $"/{rootNodeName}/vnet{largeTreeRoots[idx]}/mappings/v4ca";
                }

                // flip the flag so that the thread switches between creating small trees and large trees.
                createSmallTree = !createSmallTree;

                while (numToCreate-- > 0 && !token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < asyncTaskCount || token.IsCancellationRequested);

                    var path = $"{subtree}/{Guid.NewGuid()}";
                    var data = Helpers.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

                    var startTime = clock.Elapsed;
                    var unused = client.Create(path, data, null, CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag)
                        .ContinueWith(t =>
                        {
                            Interlocked.Decrement(ref taskCount);

                            if (t.Exception != null)
                            {
                                if (Interlocked.Increment(ref totalFailures) < PrintFailureThreshold)
                                {
                                    log($"Failed to create path: {path}");
                                }
                            }
                            else
                            {
                                Interlocked.Add(ref totalDataSize, data.Length);
                                Interlocked.Increment(ref totalDataCount);

                                var duration = clock.Elapsed - startTime;
                                MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Create);
                            }
                        });

                    Interlocked.Increment(ref taskCount);
                    dataSize += data.Length;
                    dataCount++;
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for getting full sub-tree
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static Task GetFullSubtreeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                while (queuedNodes.TryDequeue(out string path) && !token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < asyncTaskCount || token.IsCancellationRequested);
                    var startTime = clock.Elapsed;
                    var task = client.GetFullSubtree(path, true)
                        .ContinueWith(
                            t =>
                            {
                                Interlocked.Decrement(ref taskCount);

                                if (t.Exception != null)
                                {
                                    if (Interlocked.Increment(ref totalFailures) < PrintFailureThreshold)
                                    {
                                        log($"Failed to get full subtree on path {path}: {t.Exception.Message}");
                                    }
                                }
                                else
                                {
                                    var children = t.Result.Children;
                                    Interlocked.Add(ref totalDataSize, children.Sum(c => c.Data.Length));
                                    Interlocked.Add(ref totalDataCount, children.Count);

                                    var duration = (clock.Elapsed - startTime).TotalMilliseconds;
                                    MdmHelper.LogOperationDuration((long)duration, OperationType.GetFullSubtree);
                                }
                            });

                    Interlocked.Increment(ref taskCount);

                    queuedNodes.Enqueue(path);
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Work load for testing GetNode method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static Task GetNodeThread(IRingMasterRequestHandler client, CancellationToken token, int threadId)
        {
            int taskCount = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                while (queuedNodes.TryDequeue(out string path) && !token.IsCancellationRequested)
                {
                    SpinWait.SpinUntil(() => taskCount < asyncTaskCount || token.IsCancellationRequested);
                    var startTime = clock.Elapsed;
                    var task = client.GetData(path, null)
                        .ContinueWith(
                            t =>
                            {
                                Interlocked.Decrement(ref taskCount);

                                if (t.Exception != null)
                                {
                                    if (Interlocked.Increment(ref totalFailures) < PrintFailureThreshold)
                                    {
                                        log($"Failed to get {path}: {t.Exception.Message}");
                                    }
                                }
                                else
                                {
                                    var data = t.Result;
                                    Interlocked.Add(ref totalDataSize, data.Length);
                                    Interlocked.Increment(ref totalDataCount);

                                    var duration = clock.Elapsed - startTime;
                                    MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Get);
                                }
                            });

                    Interlocked.Increment(ref taskCount);

                    queuedNodes.Enqueue(path);
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Main test workflow
        /// </summary>
        /// <param name="testTitle">Title of the test case</param>
        /// <param name="operationType">the operation type</param>
        /// <param name="workload">Workload in each thread</param>
        /// <param name="durationInSeconds">How long the test should run</param>
        /// <returns>Number of operations per second</returns>
        private static async Task<double> TestFlowAsync(
            string testTitle,
            OperationType operationType,
            Func<IRingMasterRequestHandler, CancellationToken, int, Task> workload,
            int durationInSeconds)
        {
            var cancellation = new CancellationTokenSource();

            log($"Starting test {testTitle} in {threadCount} threads");

            var threads = Helpers.StartMultipleThreads(
                threadCount,
                (object n) => workload(clients[(int)n], cancellation.Token, (int)n).GetAwaiter().GetResult());

            var lastCount = Interlocked.Read(ref totalDataCount);
            var lastSize = Interlocked.Read(ref totalDataSize);

            var initialCount = lastCount;
            var initialSize = lastSize;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < durationInSeconds; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                long size = Interlocked.Read(ref totalDataSize);
                long delta = size - lastSize;

                long count = Interlocked.Read(ref totalDataCount);
                long deltaCount = count - lastCount;

                log($"{DateTime.Now} - {deltaCount} - {delta}");

                lastSize = size;
                lastCount = count;
            }

            stopwatch.Stop();
            var processedCount = Interlocked.Read(ref totalDataCount) - initialCount;
            var processedSize = Interlocked.Read(ref totalDataSize) - initialSize;
            var rate = processedCount / stopwatch.Elapsed.TotalSeconds;

            log($"Stopping test {testTitle}. Data processed {processedSize} bytes in {processedCount} ops. Failures = {totalFailures}");
            MdmHelper.LogBytesProcessed(processedSize, operationType);
            cancellation.Cancel();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            log($"Stopped {testTitle}.");

            return rate;
        }

        private static void ResetCounts()
        {
            totalDataCount = totalDataSize = totalFailures = 0;
        }

        private static void CleanupTestData()
        {
            var createdRoots = helperClient.GetChildren("/", null).GetAwaiter().GetResult();
            var rootsToDelete = new List<string>() { rootNodeName, $"{rootNodeName}_Batch", $"{rootNodeName}_Multi" };

            rootsToDelete.RemoveAll(r => !createdRoots.Contains(r));

            queuedNodes = new ConcurrentQueue<string>();
            ManualResetEvent mre = new ManualResetEvent(false);

            Task.Run(async () =>
            {
                foreach (var rootName in rootsToDelete)
                {
                    var startFrom = string.Empty;

                    while (true)
                    {
                        var children = await helperClient.GetChildren($"/{rootName}", null, $">:{MaxChildrenCount}:{startFrom}");
                        foreach (var child in children)
                        {
                            queuedNodes.Enqueue($"/{rootName}/{child}");
                            startFrom = child;
                        }

                        mre.Set();
                        if (children.Count < MaxChildrenCount)
                        {
                            break;
                        }
                    }
                }
            });

            Parallel.ForEach(clients, async client =>
            {
                mre.WaitOne();
                while (queuedNodes.TryDequeue(out string path))
                {
                    try
                    {
                        await client.Delete(path, -1, DeleteMode.CascadeDelete | DeleteMode.FastDelete | DeleteMode.SuccessEvenIfNodeDoesntExist);
                    }
                    catch (RingMasterException ex)
                    {
                        // When the tree is too large the operation may timeout. But usually the tree has been successfully deleted in the backend.
                        // So ignore this exception and go ahead to delete next tree.
                        if (ex.ErrorCode != RingMasterException.Code.ServerOperationTimeout)
                        {
                            throw;
                        }
                    }
                }
            });

            foreach (var rootName in rootsToDelete)
            {
                helperClient.Delete($"/{rootName}", -1, DeleteMode.CascadeDelete | DeleteMode.FastDelete | DeleteMode.SuccessEvenIfNodeDoesntExist).GetAwaiter().GetResult();
            }
        }
    }
}
