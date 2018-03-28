// <copyright file="VegaServiceFabricPerf.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Networking.Infrastructure.RingMaster;
    using Azure.Networking.Infrastructure.RingMaster.Data;
    using VisualStudio.TestTools.UnitTesting;
    using static TestCommon;

    /// <summary>
    /// The Vega Service Fabric Performance test class
    /// </summary>
    [TestClass]
    public class VegaServiceFabricPerf
    {
        private const int MaxChildrenCount = 256;

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
        private static RingMasterClient[] clients;

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
        /// Test setup
        /// </summary>
        /// <param name="context">test context for logging</param>
        [ClassInitialize]
        public static void TestClassInitialize(TestContext context)
        {
            minDataSize = int.Parse(ConfigurationManager.AppSettings["MinDataSize"]);
            maxDataSize = int.Parse(ConfigurationManager.AppSettings["MaxDataSize"]);
            batchOpCount = int.Parse(ConfigurationManager.AppSettings["BatchOpCount"]);
            testCaseSeconds = int.Parse(ConfigurationManager.AppSettings["TestCaseSeconds"]);
            requestTimeout = int.Parse(ConfigurationManager.AppSettings["RequestTimeout"]);
            threadCount = int.Parse(ConfigurationManager.AppSettings["ThreadCount"]);
            asyncTaskCount = int.Parse(ConfigurationManager.AppSettings["AsyncTaskCount"]);

            if (threadCount < 0)
            {
                threadCount = Environment.ProcessorCount;
            }

            log = s => context.WriteLine(s);

            var serviceInfo = TestCommon.GetVegaServiceInfo().Result;

            // Allow the endpoint address being specified from the command line
            if (context.Properties.Contains("ServerAddress"))
            {
                server = context.Properties["ServerAddress"] as string;
            }
            else
            {
                server = serviceInfo.Item1;
            }

            if (context.Properties.Contains("RootNodeName"))
            {
                rootNodeName = context.Properties["RootNodeName"] as string;
            }
            else
            {
                rootNodeName = "Perf";
            }

            clients = Enumerable.Range(0, threadCount).Select(x => new RingMasterClient(
                connectionString: server,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null))
                .ToArray();

            // Ensure the sufficient threads in the pool for the async tasks.
            ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
            ThreadPool.SetMinThreads(threadCount * 2, minIOC);

            MdmHelper.Initialize(serviceInfo.Item2);
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        [ClassCleanup]
        public static void TestClassCleanup()
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
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
                            Interlocked.Increment(ref totalFailures);
                            log($"Failed to call Batch: {ex.Message}");
                        }
                    }
                },
                testCaseSeconds)
                .GetAwaiter().GetResult();
            log($"Ping-Pong test rate: {rate:G4} /sec");
        }

        /// <summary>
        /// Get sub-tree performance
        /// </summary>
        [TestMethod]
        public void TestRingMasterGetFullSubtree()
        {
            ResetCounts();
            var rate = TestFlowAsync(
                "Get full sub-tree perf test",
                OperationType.GetFullSubtree,
                GetFullSubtreeThread,
                testCaseSeconds * 8)
                .GetAwaiter().GetResult();
            log($"Node create rate: {rate:G4} /sec");
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
                testCaseSeconds)
                .GetAwaiter().GetResult();
            log($"Node create rate: {rate:G4} /sec");
        }

        /// <summary>
        /// RingMaster get node test
        /// </summary>
        [TestMethod]
        public void TestRingMasterGet()
        {
            ResetCounts();
            var rate = TestFlowAsync(
                "Get data node perf test",
                OperationType.Get,
                GetNodeThread,
                testCaseSeconds * 2)
                .GetAwaiter().GetResult();
            log($"Node get rate: {rate:G4} /sec");
        }

        /// <summary>
        /// RingMaster get node test
        /// </summary>
        [TestMethod]
        public void TestRingMasterSet()
        {
            ResetCounts();
            var rate = TestFlowAsync(
                "Set node perf test",
                OperationType.Set,
                SetNodeThread,
                testCaseSeconds)
                .GetAwaiter().GetResult();
            log($"Node set rate: {rate:G4} /sec");
        }

        /// <summary>
        /// Tests the ring master delete.
        /// </summary>
        [TestMethod]
        public void TestRingMasterDelete()
        {
            ResetCounts();
            var rate = TestFlowAsync(
                "Delete node perf test",
                OperationType.Delete,
                async (client, cancellationToken, threadId) =>
                {
                    var rnd = new Random();

                    var path = $"/{rootNodeName}/Thread{threadId}";

                    int deleteCount = 0;

                    var watcher = new CallbackWatcher
                    {
                        OnProcess = (evtArgs) =>
                        {
                            if (evtArgs.EventType == WatchedEvent.WatchedEventType.NodeChildrenChanged)
                            {
                                deleteCount++;
                            }
                        },
                    };

                    var tasks = new ConcurrentDictionary<Task, bool>();
                    int taskCount = 0;
                    var childrenCount = 0;
                    var startFrom = string.Empty;
                    var clock = Stopwatch.StartNew();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var children = await client.GetChildren(path, watcher, $">:{MaxChildrenCount}:{startFrom}");

                        foreach (var child in children)
                        {
                            SpinWait.SpinUntil(() => taskCount < asyncTaskCount || cancellationToken.IsCancellationRequested);
                            var startTime = clock.Elapsed;
                            var task = client.Delete(string.Concat(path, "/", child), -1, DeleteMode.FastDelete)
                                   .ContinueWith(
                                       t =>
                                       {
                                           Interlocked.Decrement(ref taskCount);
                                           tasks.TryRemove(t, out var _);

                                           if (t.Exception != null)
                                           {
                                               Interlocked.Increment(ref totalFailures);
                                               log($"Failed to get {path}: {t.Exception.Message}");
                                           }
                                           else
                                           {
                                               Interlocked.Increment(ref totalDataCount);

                                               var duration = clock.Elapsed - startTime;
                                               MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Delete);
                                           }
                                       });

                            Interlocked.Increment(ref taskCount);
                            tasks.TryAdd(task, true);

                            startFrom = child;
                        }

                        childrenCount += children.Count;

                        if (children.Count < MaxChildrenCount)
                        {
                            break;
                        }
                    }

                    SpinWait.SpinUntil(() => taskCount == 0);

                    var timeoutClock = Stopwatch.StartNew();
                    while (deleteCount < childrenCount && timeoutClock.ElapsedMilliseconds < 5000)
                    {
                        await Task.Delay(1000);
                        log($"Thread {threadId} waiting for notification. Children count {childrenCount}, Notification count: {deleteCount}");
                    }

                    watcher.OnProcess = null;

                    int delta = childrenCount - deleteCount;
                    if (delta != 0)
                    {
                        log($"Thread {threadId} Children count {childrenCount} Notification count: {deleteCount}");
                    }
                },
                testCaseSeconds)
                .GetAwaiter().GetResult();

            log($"Node delete rate: {rate:G4} /sec");
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
        /// Starts multiple threads for the given action
        /// </summary>
        /// <param name="threadCount">Number of threads to be started</param>
        /// <param name="action">Thread body</param>
        /// <returns>List of threads</returns>
        internal static Thread[] StartMultipleThreads(int threadCount, ParameterizedThreadStart action)
        {
            var threads = new List<Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                threads.Add(new Thread(action));
            }

            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Start(i);
            }

            return threads.ToArray();
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
                    int seq = 0;
                    var clock = Stopwatch.StartNew();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var ops = new List<Op>(batchOpCount);
                        var totalSize = 0;

                        for (int i = 0; i < batchOpCount; i++)
                        {
                            var path = $"/{rootNodeName}/Thread{threadId}/{seq++}";
                            var data = TestCommon.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));
                            totalSize += data.Length;

                            ops.Add(Op.Create(path, data, null, CreateMode.PersistentAllowPathCreation));
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
                            Interlocked.Increment(ref totalFailures);
                            log($"Failed to call {name}: {ex.Message}");
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
        private static async Task SetNodeThread(RingMasterClient client, CancellationToken token, int threadId)
        {
            var tasks = new ConcurrentDictionary<Task, bool>();
            int taskCount = 0;
            var rnd = new Random();
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                var basePath = $"/{rootNodeName}/Thread{threadId}";
                var startFrom = string.Empty;

                while (!token.IsCancellationRequested)
                {
                    var children = await client.GetChildren(basePath, false, $">:{MaxChildrenCount}:{startFrom}").ConfigureAwait(false);

                    foreach (var child in children)
                    {
                        var path = string.Concat(basePath, "/", child);
                        var data = TestCommon.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

                        SpinWait.SpinUntil(() => taskCount < asyncTaskCount || token.IsCancellationRequested);
                        var startTime = clock.Elapsed;
                        var task = client.SetData(path, data, -1)
                             .ContinueWith(
                               t =>
                               {
                                   Interlocked.Decrement(ref taskCount);
                                   tasks.TryRemove(t, out var _);

                                   if (t.Exception != null)
                                   {
                                       Interlocked.Increment(ref totalFailures);
                                       log($"Failed to set {path}: {t.Exception.Message}");
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
                        tasks.TryAdd(task, true);

                        startFrom = child;
                    }

                    if (children.Count < MaxChildrenCount)
                    {
                        break;
                    }
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
        }

        /// <summary>
        /// Work load for testing Create method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static async Task CreateNodeThread(RingMasterClient client, CancellationToken token, int threadId)
        {
            var rnd = new Random();
            int seq = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                var dataSize = 0;
                var dataCount = 0;

                for (int i = 0; i < asyncTaskCount; i++)
                {
                    var path = $"/{rootNodeName}/Thread{threadId}/{seq++}";
                    var data = TestCommon.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

                    var startTime = clock.Elapsed;
                    tasks.Add(client.Create(path, data, null, CreateMode.PersistentAllowPathCreation)
                        .ContinueWith(t =>
                        {
                            var duration = clock.Elapsed - startTime;
                            MdmHelper.LogOperationDuration((long)duration.TotalMilliseconds, OperationType.Create);
                        }));

                    dataSize += data.Length;
                    dataCount++;
                }

                try
                {
                    await Task.WhenAll(tasks);
                    Interlocked.Add(ref totalDataSize, dataSize);
                    Interlocked.Add(ref totalDataCount, dataCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref totalFailures);
                    log($"Failed to create node in {threadId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Work load for getting full sub-tree
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static async Task GetFullSubtreeThread(RingMasterClient client, CancellationToken token, int threadId)
        {
            var rnd = new Random();
            int seq = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                var path = $"/{rootNodeName}/Thread{threadId}-{seq++}";
                var data = TestCommon.MakeSequentialData(rnd.Next(minDataSize, maxDataSize));

                try
                {
                    await client.Create(path, data, null, CreateMode.PersistentAllowPathCreation, false).ConfigureAwait(false);

                    Interlocked.Add(ref totalDataSize, data.Length);
                    Interlocked.Increment(ref totalDataCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref totalFailures);
                    log($"Failed to create {path}: {ex.Message}");
                }

                if (seq % 16 == 0 && threadId < 8)
                {
                    try
                    {
                        path = $"/{rootNodeName}";

                        var startTime = clock.Elapsed;
                        var rootNode = await client.GetFullSubtree(path, true).ConfigureAwait(false);
                        var count = rootNode.Children.Count;
                        var duration = (clock.Elapsed - startTime).TotalMilliseconds;
                        MdmHelper.LogOperationDuration((long)duration, OperationType.GetFullSubtree);

                        var rate = count * 1000 / duration;
                        log($"Thread {threadId}: seq={seq} duration={duration:F1}ms count={count} rate={rate:G4}");
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref totalFailures);
                        log($"Failed to GetFullSubtree {path}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Work load for testing GetNode method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static async Task GetNodeThread(RingMasterClient client, CancellationToken token, int threadId)
        {
            var tasks = new ConcurrentDictionary<Task, bool>();
            int taskCount = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                var basePath = $"/{rootNodeName}/Thread{threadId}";
                var startFrom = string.Empty;

                while (!token.IsCancellationRequested)
                {
                    var children = await client.GetChildren(basePath, false, $">:{MaxChildrenCount}:{startFrom}");

                    foreach (var child in children)
                    {
                        var path = string.Concat(basePath, "/", child);

                        SpinWait.SpinUntil(() => taskCount < asyncTaskCount || token.IsCancellationRequested);
                        var startTime = clock.Elapsed;
                        var task = client.GetData(path, false)
                            .ContinueWith(
                                t =>
                                {
                                    Interlocked.Decrement(ref taskCount);
                                    tasks.TryRemove(t, out var _);

                                    if (t.Exception != null)
                                    {
                                        Interlocked.Increment(ref totalFailures);
                                        log($"Failed to get {path}: {t.Exception.Message}");
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
                        tasks.TryAdd(task, true);

                        startFrom = child;
                    }

                    if (children.Count < MaxChildrenCount)
                    {
                        break;
                    }
                }
            }

            SpinWait.SpinUntil(() => taskCount == 0);
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
            Func<RingMasterClient, CancellationToken, int, Task> workload,
            int durationInSeconds)
        {
            var cancellation = new CancellationTokenSource();

            log($"Starting test {testTitle} in {threadCount} threads");

            var threads = StartMultipleThreads(
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
    }
}
