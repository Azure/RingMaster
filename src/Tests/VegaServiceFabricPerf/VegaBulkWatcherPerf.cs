// <copyright file="VegaBulkWatcherPerf.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Vega.Test.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        private static IConfiguration appSettings;

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
        /// Setup of trace log at the assembly level
        /// </summary>
        /// <param name="context">Test context</param>
        [AssemblyInitialize]
        public static void Setup(TestContext context)
        {
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            appSettings = builder.Build();

            Helpers.SetupTraceLog(Path.Combine(appSettings["LogFolder"], "VegaServiceFabricPerf.LogPath"));
        }

        /// <summary>
        /// Test setup
        /// </summary>
        /// <param name="context">test context for logging</param>
        [ClassInitialize]
        public static void TestClassInitialize(TestContext context)
        {
            threadCount = int.Parse(appSettings["BulkWatcherThreadCount"]);
            testRepetitions = int.Parse(appSettings["TestRepetitions"]);
            minDataSize = int.Parse(appSettings["BulkWatcherMinDataSize"]);
            maxDataSize = int.Parse(appSettings["BulkWatcherMaxDataSize"]);
            requestTimeout = int.Parse(appSettings["BulkWatcherRequestTimeout"]);
            partitionCount = int.Parse(appSettings["PartitionCount"]);
            nodeCountPerPartition = int.Parse(appSettings["NodeCountPerPartition"]);
            channelCount = int.Parse(appSettings["ChannelCount"]);
            server = appSettings["ServerAddress"];

            if (context.GetType().Name.StartsWith("Dummy"))
            {
                log = s => context.WriteLine(s);
            }
            else
            {
                log = s => Trace.TraceInformation(s);
            }

            if (threadCount < 0)
            {
                threadCount = Environment.ProcessorCount;
            }

            var serviceInfo = default(Tuple<string, string>);

            if (string.IsNullOrEmpty(server))
            {
                // Only queries the service info if the service address is not specified, e.g. when running in CloudTest
                // on local machine.
                serviceInfo = Helpers.GetVegaServiceInfo().Result;
                server = serviceInfo.Item1;
            }

            clients = Enumerable.Range(0, channelCount).Select(n => new RingMasterClient(
                connectionString: server,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null))
                .ToArray();

            VegaServiceFabricPerf.InitializeMdm(serviceInfo?.Item2, appSettings);
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

            var cancelShowProgress = new CancellationTokenSource();
            _ = ShowProgress(cancelShowProgress.Token);

            if (!await this.CreateNodeTree(cancellation).ConfigureAwait(false))
            {
                cancelShowProgress.Cancel();
                cancellationSource.Cancel();

                Assert.Fail($"No progress in CreateNodeTree after {requestTimeout} ms");
            }

            log("Reading all nodes...");
            if (!await this.ReadNodeTree(cancellation).ConfigureAwait(false))
            {
                cancelShowProgress.Cancel();
                cancellationSource.Cancel();

                Assert.Fail($"No progress in ReadNodeTree after {requestTimeout} ms");
            }

            cancelShowProgress.Cancel();

            var watchers = new ConcurrentBag<IWatcher>();
            int watcherTriggerCount = 0;
            long watcherDataDelivered = 0;
            var startTime = stopwatch.Elapsed;

            log($"Installing bulk watchers...");

            long watcherId = 0;
            var installTask = Helpers.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    foreach (var client in clients)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            break;
                        }

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
                threadCount);

            await Task.WhenAny(installTask, Task.Delay(requestTimeout)).ConfigureAwait(false);
            if (!installTask.IsCompleted && watcherTriggerCount == 0)
            {
                cancellationSource.Cancel();
                Assert.Fail($"No watcher event received after {requestTimeout} ms");
            }

            var duration = (stopwatch.Elapsed - startTime).TotalSeconds;
            var installRate = watchers.Count / duration;
            log($"Finished installing bulk watchers in {duration:F3} sec. Rate: {installRate:G4} /sec");
            MdmHelper.LogWatcherCountProcessed(watchers.Count, OperationType.InstallBulkWatcher);

            // Make some random change, one node in each partition, and wait for watcher being triggered
            for (int i = 0; i < testRepetitions && !cancellation.IsCancellationRequested; i++)
            {
                startTime = stopwatch.Elapsed;
                totalDataSize = 0;
                watcherTriggerCount = 0;
                watcherDataDelivered = 0;

                var unused1 = Task.Run(() => this.ChangeRandomNodeInTree());

                var timeoutClock = Stopwatch.StartNew();
                while (watcherTriggerCount < partitionCount * clients.Length
                    && timeoutClock.ElapsedMilliseconds < 30 * 1000
                    && !cancellation.IsCancellationRequested)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    log($" -- watcher event received: {watcherTriggerCount}, data received: {watcherDataDelivered}");
                }

                duration = (stopwatch.Elapsed - startTime).TotalSeconds;
                log($"Iteration {i} - {watcherTriggerCount} events / {watcherDataDelivered} bytes received in {duration} seconds. Read {totalDataSize} bytes.");
                MdmHelper.LogWatcherCountProcessed(watcherTriggerCount, OperationType.BulkWatcherTrigger);
                MdmHelper.LogOperationDuration((long)(duration * 1000), OperationType.BulkWatcherTrigger);
                MdmHelper.LogBytesProcessed(watcherDataDelivered, OperationType.BulkWatcherTrigger);
            }

            Assert.IsTrue(await this.DeleteNodeTree(cancellation).ConfigureAwait(false));
        }

        private async Task<bool> CreateNodeTree(CancellationToken cancellation)
        {
            totalDataCount = 0;
            totalDataSize = 0;
            var startTime = stopwatch.Elapsed;

            var createTask = Helpers.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    var rnd = new Random();

                    for (int i = 0; i < nodeCountPerPartition && !cancellation.IsCancellationRequested; i++)
                    {
                        var path = $"{PartitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{i}";
                        var data = Helpers.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

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
                threadCount);

            await Task.WhenAny(createTask, Task.Delay(requestTimeout)).ConfigureAwait(false);
            if (!createTask.IsCompleted && totalDataCount == 0)
            {
                // If no data is created successfully within timeout time, don't bother to read any more.
                return false;
            }
            else
            {
                // It is making progress. Wait until it's completed.
                await createTask.ConfigureAwait(false);
            }

            var duration = (stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = totalDataSize / duration;
            var qps = totalDataCount / duration;
            log($"{nameof(this.CreateNodeTree)}: {totalDataCount} nodes created, total data size is {totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(totalDataSize, OperationType.BulkWatcherCreateNode);

            return true;
        }

        private async Task ChangeRandomNodeInTree()
        {
            totalDataCount = 0;
            totalDataSize = 0;
            var startTime = stopwatch.Elapsed;

            await Helpers.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    var rnd = new Random();

                    var index = rnd.Next(nodeCountPerPartition);
                    var path = $"{PartitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{index}";
                    var data = Helpers.MakeRandomData(rnd, rnd.Next(minDataSize, maxDataSize));

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

        private async Task<bool> ReadNodeTree(CancellationToken cancellation)
        {
            totalDataCount = 0;
            totalDataSize = 0;
            var startTime = stopwatch.Elapsed;

            var readTask = Helpers.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    for (int i = 0; i < nodeCountPerPartition && !cancellation.IsCancellationRequested; i++)
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
                threadCount);

            await Task.WhenAny(readTask, Task.Delay(requestTimeout)).ConfigureAwait(false);
            if (!readTask.IsCompleted && totalDataCount == 0)
            {
                // If no data is read successfully within timeout time, don't bother to read any more.
                return false;
            }
            else
            {
                // It is making progress. Wait until it's completed.
                await readTask.ConfigureAwait(false);
            }

            var duration = (stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = totalDataSize / duration;
            var qps = totalDataCount / duration;
            log($"{nameof(this.ReadNodeTree)}: {totalDataCount} nodes read, total data size is {totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(totalDataSize, OperationType.BulkWatcherReadNode);

            return true;
        }

        private async Task<bool> DeleteNodeTree(CancellationToken cancellation)
        {
            totalDataCount = 0;

            var deleteTask = Helpers.ForEachAsync(
                Enumerable.Range(0, partitionCount),
                async (partitionIndex) =>
                {
                    for (int i = 0; i < nodeCountPerPartition && !cancellation.IsCancellationRequested; i++)
                    {
                        var path = $"{PartitionKeyPrefix}{partitionIndex}/";

                        try
                        {
                            await clients[partitionIndex % channelCount].Delete(path, -1, DeleteMode.CascadeDelete).ConfigureAwait(false);

                            Interlocked.Increment(ref totalDataCount);
                        }
                        catch (Exception ex)
                        {
                            log($"Failed to delete {path}: {ex}");
                        }
                    }
                },
                threadCount);

            await Task.WhenAny(deleteTask, Task.Delay(requestTimeout)).ConfigureAwait(false);
            if (!deleteTask.IsCompleted && totalDataCount == 0)
            {
                // If no data is deleted successfully within timeout time, don't bother to read any more.
                return false;
            }
            else
            {
                // It is making progress. Wait until it's completed.
                await deleteTask.ConfigureAwait(false);
            }

            log($"DeleteNodeTree is completed.");
            return true;
        }
    }
}
