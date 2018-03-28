// <copyright file="VegaBulkWatcherPerf.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using static TestCommon;

    /// <summary>
    /// The Vega Bulk Watcher Performance test class
    /// </summary>
    [TestClass]
    public class VegaBulkWatcherPerf
    {
        /// <summary>
        /// Prefix of the partition key, e.g. <h ref="/VegaUserData/%02Fvnets123456"/>
        /// </summary>
        private const string PartitionKeyPrefix = "/VegaUserData/%2Fvnets";

        /// <summary>
        /// Prefix of the relative path, e.g. <h ref="/mappings/v4ca/123456" />
        /// </summary>
        private const string RelativePathPrefix = "mappings/v4ca";

        /// <summary>
        /// Number of times to repeat the test and wait for notification
        /// </summary>
        private static int testRepetitions = 100;

        /// <summary>
        /// By default use -1 to use the same number of processor count. Higher number will cause Ring Master timeout.
        /// </summary>
        private static int threadCount = -1;

        /// <summary>
        /// Total number of partitions
        /// </summary>
        private static int partitionCount = 10 * 1024;

        /// <summary>
        /// Number of nodes under each individual relative path
        /// </summary>
        private static int nodeCountPerPartition = 16;

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
        private static int requestTimeout = 100 * 1000;

        /// <summary>
        /// Simulate multiple notification services having multiple connections to the backend
        /// </summary>
        private static int channelCount = 10;

        /// <summary>
        /// Logging delegate
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Endpoint of the backend
        /// </summary>
        private static string server;

        /// <summary>
        /// RingMaster clients
        /// </summary>
        private static RingMasterClient[] clients;

        /// <summary>
        /// Total amount of data being processed
        /// </summary>
        private static long totalDataSize = 0;

        /// <summary>
        /// Total number of data items being processed
        /// </summary>
        private static long totalDataCount = 0;

        /// <summary>
        /// Global high-precision stop watch
        /// </summary>
        private static Stopwatch stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Test setup
        /// </summary>
        /// <param name="context">test context for logging</param>
        [ClassInitialize]
        public static void TestClassInitialize(TestContext context)
        {
            log = s => context.WriteLine("{0} {1}", DateTime.UtcNow, s);

            threadCount = int.Parse(ConfigurationManager.AppSettings["BulkWatcherThreadCount"]);
            testRepetitions = int.Parse(ConfigurationManager.AppSettings["TestRepetitions"]);
            minDataSize = int.Parse(ConfigurationManager.AppSettings["BulkWatcherMinDataSize"]);
            maxDataSize = int.Parse(ConfigurationManager.AppSettings["BulkWatcherMaxDataSize"]);
            requestTimeout = int.Parse(ConfigurationManager.AppSettings["BulkWatcherRequestTimeout"]);
            partitionCount = int.Parse(ConfigurationManager.AppSettings["PartitionCount"]);
            nodeCountPerPartition = int.Parse(ConfigurationManager.AppSettings["NodeCountPerPartition"]);
            channelCount = int.Parse(ConfigurationManager.AppSettings["ChannelCount"]);

            if (threadCount < 0)
            {
                threadCount = Environment.ProcessorCount;
            }

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

            clients = Enumerable.Range(0, channelCount).Select(n => new RingMasterClient(
                connectionString: server,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null))
                .ToArray();

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
        /// Tests the bulk watcher.
        /// </summary>
        [TestMethod]
        public void TestBulkWatcher()
        {
            this.TestBulkWatcherAsync().GetAwaiter().GetResult();
        }

        private static async Task ShowProgress(CancellationToken cancellation)
        {
            var lastCount = Interlocked.Read(ref totalDataCount);
            var lastSize = Interlocked.Read(ref totalDataSize);

            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(1000).ConfigureAwait(false);

                var count = Interlocked.Read(ref totalDataCount);
                var size = Interlocked.Read(ref totalDataSize);
                log($"Count={count} +{count - lastCount} Size={size / 1024}k +{(size - lastSize) / 1024}k");

                lastCount = count;
                lastSize = size;
            }
        }

        private async Task TestBulkWatcherAsync()
        {
            var cancellationSource = new CancellationTokenSource();
            var cancellation = cancellationSource.Token;

            var totalSizeMB = 0.5 * (maxDataSize + minDataSize) * partitionCount * nodeCountPerPartition / 1024 / 1024;
            log($"Creating {partitionCount} partitions, {nodeCountPerPartition} nodes in each partition, total amount of data {totalSizeMB} MB");

            var cancel = new CancellationTokenSource();
            var unused = ShowProgress(cancel.Token);
            await this.CreateNodeTree().ConfigureAwait(false);

            log("Reading all nodes...");
            await this.ReadNodeTree().ConfigureAwait(false);
            cancel.Cancel();

            var watchers = new ConcurrentBag<IWatcher>();
            int watcherTriggerCount = 0;
            long watcherDataDelivered = 0;
            var startTime = stopwatch.Elapsed;

            log($"Installing bulk watchers...");

            long watcherId = 0;
            await TestCommon.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    foreach (var client in clients)
                    {
                        var path = $"{PartitionKeyPrefix}{partitionIndex}";
                        var watcher = new CallbackWatcher
                        {
                            OnProcess = (watchedEvent) =>
                            {
                                if (watchedEvent.EventType == WatchedEvent.WatchedEventType.NodeDataChanged)
                                {
                                    Interlocked.Add(ref watcherDataDelivered, watchedEvent.Data.Length);
                                    Interlocked.Increment(ref watcherTriggerCount);
                                }
                                else if (watchedEvent.EventType != WatchedEvent.WatchedEventType.WatcherRemoved)
                                {
                                    log($" -- {watchedEvent.EventType} / {watchedEvent.KeeperState} - {watchedEvent.Path}");
                                }
                            },
                            Id = (ulong)Interlocked.Increment(ref watcherId),
                        };
                        try
                        {
                            var operationStartTime = stopwatch.Elapsed;
                            await client.RegisterBulkWatcher(path, watcher).ConfigureAwait(false);
                            var operationDuration = stopwatch.Elapsed - operationStartTime;
                            MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, OperationType.InstallBulkWatcher);

                            watchers.Add(watcher);
                        }
                        catch (Exception ex)
                        {
                            log($"  Watcher at path {path} failed to install: {ex.Message}");
                        }
                    }
                },
                threadCount)
                .ConfigureAwait(false);

            var duration = (stopwatch.Elapsed - startTime).TotalSeconds;
            var installRate = watchers.Count / duration;
            log($"Finished installing bulk watchers in {duration:F3} sec. Rate: {installRate:G4} /sec");
            MdmHelper.LogWatcherCountProcessed(watchers.Count, OperationType.InstallBulkWatcher);

            // Make some random change, one node in each partition, and wait for watcher being triggered
            for (int i = 0; i < testRepetitions; i++)
            {
                startTime = stopwatch.Elapsed;
                totalDataSize = 0;
                watcherTriggerCount = 0;
                watcherDataDelivered = 0;

                var unused1 = Task.Run(() => this.ChangeRandomNodeInTree());

                var timeoutClock = Stopwatch.StartNew();
                while (watcherTriggerCount < partitionCount * clients.Length && timeoutClock.ElapsedMilliseconds < 30 * 1000)
                {
                    await Task.Delay(1000, cancellation).ConfigureAwait(false);
                    log($" -- watcher event received: {watcherTriggerCount}, data received: {watcherDataDelivered}");
                }

                duration = (stopwatch.Elapsed - startTime).TotalSeconds;
                log($"Iteration {i} - {watcherTriggerCount} events / {watcherDataDelivered} bytes received in {duration} seconds. Read {totalDataSize} bytes.");
                MdmHelper.LogWatcherCountProcessed(watcherTriggerCount, OperationType.BulkWatcherTrigger);
                MdmHelper.LogOperationDuration((long)(duration * 1000), OperationType.BulkWatcherTrigger);
                MdmHelper.LogBytesProcessed(watcherDataDelivered, OperationType.BulkWatcherTrigger);
            }

            await this.DeleteNodeTree().ConfigureAwait(false);
        }

        private async Task CreateNodeTree()
        {
            totalDataCount = 0;
            totalDataSize = 0;
            var startTime = stopwatch.Elapsed;

            await TestCommon.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    var rnd = new Random();

                    for (int i = 0; i < nodeCountPerPartition; i++)
                    {
                        var path = $"{PartitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{i}";
                        var data = TestCommon.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

                        try
                        {
                            var operationStartTime = stopwatch.Elapsed;
                            await clients[partitionCount % channelCount].Create(path, data, null, CreateMode.PersistentAllowPathCreation, false).ConfigureAwait(false);
                            var operationDuration = stopwatch.Elapsed - operationStartTime;
                            MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, OperationType.BulkWatcherCreateNode);

                            Interlocked.Add(ref totalDataSize, data.Length);
                            Interlocked.Increment(ref totalDataCount);
                        }
                        catch (Exception ex)
                        {
                            log($"Failed to create {path}: {ex.Message}");
                        }
                    }
                },
                threadCount)
                .ConfigureAwait(false);

            var duration = (stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = totalDataSize / duration;
            var qps = totalDataCount / duration;
            log($"{nameof(this.CreateNodeTree)}: {totalDataCount} nodes created, total data size is {totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(totalDataSize, OperationType.BulkWatcherCreateNode);
        }

        private async Task ChangeRandomNodeInTree()
        {
            totalDataCount = 0;
            totalDataSize = 0;
            var startTime = stopwatch.Elapsed;

            await TestCommon.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    var rnd = new Random();

                    var index = rnd.Next(nodeCountPerPartition);
                    var path = $"{PartitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{index}";
                    var data = MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

                    try
                    {
                        var operationStartTime = stopwatch.Elapsed;
                        await clients[partitionIndex % channelCount].SetData(path, data, -1).ConfigureAwait(false);
                        var operationDuration = stopwatch.Elapsed - operationStartTime;
                        MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, OperationType.BulkWatcherChangeNode);

                        Interlocked.Add(ref totalDataSize, data.Length);
                        Interlocked.Increment(ref totalDataCount);
                    }
                    catch (Exception ex)
                    {
                        log($"Failed to set {path}: {ex.Message}");
                    }
                },
                threadCount)
                .ConfigureAwait(false);

            var duration = (stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = totalDataSize / duration;
            var qps = totalDataCount / duration;
            log($"{nameof(this.ChangeRandomNodeInTree)}: {totalDataCount} nodes updated, total data size is {totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(totalDataSize, OperationType.BulkWatcherChangeNode);
        }

        private async Task ReadNodeTree()
        {
            totalDataCount = 0;
            totalDataSize = 0;
            var startTime = stopwatch.Elapsed;

            await TestCommon.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    for (int i = 0; i < nodeCountPerPartition; i++)
                    {
                        var path = $"{PartitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{i}";

                        try
                        {
                            var operationStartTime = stopwatch.Elapsed;
                            var data = await clients[partitionCount % channelCount].GetData(path, false).ConfigureAwait(false);
                            var operationDuration = stopwatch.Elapsed - operationStartTime;
                            MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, OperationType.BulkWatcherReadNode);

                            Interlocked.Add(ref totalDataSize, data.Length);
                            Interlocked.Increment(ref totalDataCount);
                        }
                        catch (Exception ex)
                        {
                            log($"Failed to read {path}: {ex.Message}");
                        }
                    }
                },
                threadCount)
                .ConfigureAwait(false);

            var duration = (stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = totalDataSize / duration;
            var qps = totalDataCount / duration;
            log($"{nameof(this.ReadNodeTree)}: {totalDataCount} nodes read, total data size is {totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(totalDataSize, OperationType.BulkWatcherReadNode);
        }

        private async Task DeleteNodeTree()
        {
            await TestCommon.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    for (int i = 0; i < nodeCountPerPartition; i++)
                    {
                        var path = $"{PartitionKeyPrefix}{partitionIndex}/";

                        try
                        {
                            await clients[partitionIndex % channelCount].Delete(path, -1, DeleteMode.CascadeDelete).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            log($"Failed to delete {path}: {ex}");
                        }
                    }
                },
                threadCount)
                .ConfigureAwait(false);

            log($"DeleteNodeTree is completed.");
        }
    }
}
