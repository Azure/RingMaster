// <copyright file="VegaServiceFabricPerf.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
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
        /// <summary>
        /// Logging delegate
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Endpoint of the backend
        /// </summary>
        private static string server;

        /// <summary>
        /// RingMaster client
        /// </summary>
        private static RingMasterClient client;

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
        /// Total amount of data being processed
        /// </summary>
        private static long totalDataSize = 0;

        /// <summary>
        /// Total number of data items being processed
        /// </summary>
        private static long totalDataCount = 0;

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

            log = s => context.WriteLine(s);

            server = TestCommon.GetVegaServiceEndpoint().Result;

            client = new RingMasterClient(
                connectionString: server,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null);
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        [ClassCleanup]
        public static void TestClassCleanup()
        {
            client.Dispose();
        }

        /// <summary>
        /// Get sub-tree performance
        /// </summary>
        [TestMethod]
        public void TestRingMasterGetFullSubtree()
        {
            totalDataCount = totalDataSize = 0;
            var rate = this.TestFlowAsync(
                "Get full sub-tree perf test",
                GetFullSubtreeThread,
                testCaseSeconds * 8)
                .GetAwaiter().GetResult();
            log($"Node create rate: {rate} /sec");
        }

        /// <summary>
        /// Create data node performance
        /// </summary>
        [TestMethod]
        public void TestRingMasterCreate()
        {
            totalDataCount = totalDataSize = 0;
            var rate = this.TestFlowAsync(
                "Create data node perf test",
                CreateNodeThread,
                testCaseSeconds)
                .GetAwaiter().GetResult();
            log($"Node create rate: {rate} /sec");
        }

        /// <summary>
        /// RingMaster get node test
        /// </summary>
        [TestMethod]
        public void TestRingMasterGet()
        {
            totalDataCount = totalDataSize = 0;
            var rate = this.TestFlowAsync(
                "Get data node perf test",
                GetNodeThread,
                testCaseSeconds * 2)
                .GetAwaiter().GetResult();
            log($"Node get rate: {rate} /sec");
        }

        /// <summary>
        /// RingMaster get node test
        /// </summary>
        [TestMethod]
        public void TestRingMasterSet()
        {
            totalDataCount = totalDataSize = 0;
            var rate = this.TestFlowAsync(
                "Set node perf test",
                SetNodeThread,
                testCaseSeconds)
                .GetAwaiter().GetResult();
            log($"Node set rate: {rate} /sec");
        }

        /// <summary>
        /// Tests the ring master delete.
        /// </summary>
        [TestMethod]
        public void TestRingMasterDelete()
        {
            totalDataCount = totalDataSize = 0;
            var rate = this.TestFlowAsync(
                "Delete node perf test",
                async (client, cancellationToken, threadId) =>
                {
                    var pid = Process.GetCurrentProcess().Id;
                    var rnd = new Random();

                    var path = $"/Proc{pid}/Thread{threadId}";

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

                    var children = await client.GetChildren(path, watcher);

                    foreach (var child in children)
                    {
                        try
                        {
                            var data = await client.Delete(string.Concat(path, "/", child), -1, DeleteMode.FastDelete);
                            Interlocked.Increment(ref totalDataCount);
                        }
                        catch (Exception ex)
                        {
                            log($"Failed delete {child} under {path}: {ex}");
                        }
                    }

                    watcher.OnProcess = null;

                    int delta = children.Count - deleteCount;
                    if (delta != 0)
                    {
                        log($"Thread {threadId} Children count {children.Count} Notification count: {deleteCount}");
                    }
                },
                testCaseSeconds)
                .GetAwaiter().GetResult();

            log($"Node delete rate: {rate} /sec");
        }

        /// <summary>
        /// Test the batched creation
        /// </summary>
        [TestMethod]
        public void TestRingMasterBatchCreate()
        {
            totalDataCount = totalDataSize = 0;
            var rate = this.TestFlowAsync(
                "Batch(Create) node perf test",
                async (client, cancellationToken, threadId) =>
                {
                    var pid = Process.GetCurrentProcess().Id;
                    var rnd = new Random();
                    int seq = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var ops = new List<Op>(batchOpCount);
                        var totalSize = 0;

                        for (int i = 0; i < batchOpCount; i++)
                        {
                            var path = $"/Proc{pid}/Thread{threadId}/{seq++}";
                            var data = TestCommon.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));
                            totalSize += data.Length;

                            ops.Add(Op.Create(path, data, null, CreateMode.PersistentAllowPathCreation));
                        }

                        try
                        {
                            await client.Batch(ops).ConfigureAwait(false);

                            Interlocked.Add(ref totalDataSize, totalSize);
                            Interlocked.Add(ref totalDataCount, ops.Count);
                        }
                        catch (Exception ex)
                        {
                            log($"Failed to call Batch: {ex}");
                        }
                    }
                },
                testCaseSeconds)
                .GetAwaiter().GetResult();

            log($"Node batch create rate: {rate} /sec");
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
        /// Work load for testing GetNode method
        /// </summary>
        /// <param name="client">RingMasterClient object</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>Async task</returns>
        private static async Task SetNodeThread(RingMasterClient client, CancellationToken token, int threadId)
        {
            var pid = Process.GetCurrentProcess().Id;
            var rnd = new Random();

            while (!token.IsCancellationRequested)
            {
                var path = $"/Proc{pid}/Thread{threadId}";
                var children = await client.GetChildren(path, false).ConfigureAwait(false);

                foreach (var child in children)
                {
                    var data = TestCommon.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));
                    await client.SetData(string.Concat(path, "/", child), data, -1).ConfigureAwait(false);

                    Interlocked.Add(ref totalDataSize, data.Length);
                    Interlocked.Increment(ref totalDataCount);
                }
            }
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
            var pid = Process.GetCurrentProcess().Id;
            var rnd = new Random();
            int seq = 0;
            while (!token.IsCancellationRequested)
            {
                var path = $"/Proc{pid}/Thread{threadId}/{seq++}";
                var data = TestCommon.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));
                try
                {
                    await client.Create(path, data, null, CreateMode.PersistentAllowPathCreation).ConfigureAwait(false);

                    Interlocked.Add(ref totalDataSize, data.Length);
                    Interlocked.Increment(ref totalDataCount);
                }
                catch (Exception ex)
                {
                    log($"Failed to set {path}: {ex}");
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
            var pid = Process.GetCurrentProcess().Id;
            var rnd = new Random();
            int seq = 0;
            var clock = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                var path = $"/Proc{pid}/Thread{threadId}-{seq++}";
                var data = TestCommon.MakeSequentialData(rnd.Next(64, 256));

                try
                {
                    await client.Create(path, data, null, CreateMode.PersistentAllowPathCreation).ConfigureAwait(false);

                    Interlocked.Add(ref totalDataSize, data.Length);
                    Interlocked.Increment(ref totalDataCount);

                    if (seq % 16 == 0 && threadId < 8)
                    {
                        var root = $"/Proc{pid}";
                        var startTime = clock.Elapsed;
                        var rootNode = await client.GetFullSubtree(root, true).ConfigureAwait(false);
                        var count = rootNode.Children.Count;
                        var duration = (clock.Elapsed - startTime).TotalMilliseconds;

                        log($"Thread {threadId}: seq={seq} duration={duration}ms count={count} rate={count * 1000 / duration}");
                    }
                }
                catch (Exception ex)
                {
                    log($"Failed to set {path}: {ex}");
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
            var pid = Process.GetCurrentProcess().Id;

            while (!token.IsCancellationRequested)
            {
                var path = $"/Proc{pid}/Thread{threadId}";
                var children = await client.GetChildren(path, false).ConfigureAwait(false);

                foreach (var child in children)
                {
                    var data = await client.GetData(string.Concat(path, "/", child), false).ConfigureAwait(false);

                    Interlocked.Add(ref totalDataSize, data.Length);
                    Interlocked.Increment(ref totalDataCount);
                }
            }
        }

        /// <summary>
        /// Main test workflow
        /// </summary>
        /// <param name="testTitle">Title of the test case</param>
        /// <param name="workload">Workload in each thread</param>
        /// <param name="durationInSeconds">How long the test should run</param>
        /// <returns>Number of operations per second</returns>
        private async Task<double> TestFlowAsync(
            string testTitle,
            Func<RingMasterClient, CancellationToken, int, Task> workload,
            int durationInSeconds)
        {
            var cancellation = new CancellationTokenSource();

            log($"Starting test {testTitle} in {threadCount} threads");

            var threads = StartMultipleThreads(
                threadCount,
                (object n) => workload(client, cancellation.Token, (int)n).GetAwaiter().GetResult());

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

                log($"{DateTime.Now} - {count - lastCount} - {delta}");

                lastSize = size;
                lastCount = count;
            }

            stopwatch.Stop();
            var processedCount = Interlocked.Read(ref totalDataCount) - initialCount;
            var processedSize = Interlocked.Read(ref totalDataSize) - initialSize;
            var rate = processedCount / stopwatch.Elapsed.TotalSeconds;

            log($"Stopping test {testTitle}. Data processed {processedSize} bytes in {processedCount}");
            cancellation.Cancel();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            log($"Stopped {testTitle}.");

            return rate;
        }
    }
}
