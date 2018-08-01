// <copyright file="TestPublishSubscribeScenario.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Vega.Test.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Extensions.Configuration;
    using DistTestCommonProto;

    /// <summary>
    /// Test the Publish Subscribe Scenario
    /// </summary>
    internal sealed class TestPublishSubscribeScenario : ITestJob
    {
        /// <summary>
        /// Prefix of the relative path, e.g. <h ref="/mappings/v4ca/123456" />
        /// </summary>
        private const string RelativePathPrefix = "mappings/v4ca";

        /// <summary>
        /// Prefix of the partition key
        /// </summary>
        private string partitionKeyPrefix;

        /// <summary>
        /// Number of times to repeat the test and wait for notification
        /// </summary>
        private int testRepetitions = 100;

        /// <summary>
        /// By default use -1 to use the same number of processor count. Higher number will cause Ring Master timeout.
        /// </summary>
        private int threadCount = -1;

        /// <summary>
        /// Total number of partitions
        /// </summary>
        private int partitionCount = 10 * 1024;

        /// <summary>
        /// Number of nodes under each individual relative path
        /// </summary>
        private int nodeCountPerPartition = 16;

        /// <summary>
        /// Minimum data payload size
        /// </summary>
        private int minDataSize = 256;

        /// <summary>
        /// Maximum data payload size
        /// </summary>
        private int maxDataSize = 16384;

        /// <summary>
        /// Request timeout to the backend
        /// </summary>
        private int requestTimeout = 100 * 1000;

        /// <summary>
        /// Simulate multiple notification services having multiple connections to the backend
        /// </summary>
        private int channelCount = 10;

        /// <summary>
        /// Logging delegate
        /// </summary>
        private Action<string> log;

        /// <summary>
        /// Endpoint of the backend
        /// </summary>
        private string server;

        /// <summary>
        /// RingMaster clients
        /// </summary>
        private IRingMasterRequestHandler[] clients;

        /// <summary>
        /// Total amount of data being processed
        /// </summary>
        private long totalDataSize = 0;

        /// <summary>
        /// Total number of data items being processed
        /// </summary>
        private long totalDataCount = 0;

        /// <summary>
        /// Global high-precision stop watch
        /// </summary>
        private Stopwatch stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Gets or sets the service context.
        /// </summary>
        private StatelessServiceContext ServiceContext { get; set; }

        private ConcurrentBag<double> CreateLatency { get; } = new ConcurrentBag<double>();

        private ConcurrentBag<double> ReadLatency { get; } = new ConcurrentBag<double>();

        private ConcurrentBag<double> SetLatency { get; } = new ConcurrentBag<double>();

        private ConcurrentBag<double> DeleteLatency { get; } = new ConcurrentBag<double>();

        private ConcurrentBag<double> InstallWatcherLatency { get; } = new ConcurrentBag<double>();

        /// <summary>
        /// Initializes a new test job from the given parameters
        /// </summary>
        /// <param name="parameters">Test execution parameters</param>
        /// <param name="serviceContext">service context</param>
        /// <returns>async task</returns>
        public Task Initialize(Dictionary<string, string> parameters, StatelessServiceContext serviceContext)
        {
            this.log = s => VegaDistTestEventSource.Log.GeneralMessage(s);

            this.threadCount = int.Parse(parameters["ThreadCount"]);
            this.testRepetitions = int.Parse(parameters["TestRepetitions"]);
            this.minDataSize = int.Parse(parameters["MinDataSize"]);
            this.maxDataSize = int.Parse(parameters["MaxDataSize"]);
            this.requestTimeout = int.Parse(parameters["RequestTimeout"]);
            this.partitionCount = int.Parse(parameters["PartitionCount"]);
            this.nodeCountPerPartition = int.Parse(parameters["NodeCountPerPartition"]);
            this.channelCount = int.Parse(parameters["ChannelCount"]);
            this.ServiceContext = serviceContext;

            this.partitionKeyPrefix = $"/{nameof(TestPublishSubscribeScenario)}/{serviceContext.ReplicaOrInstanceId}";

            if (this.threadCount < 0)
            {
                this.threadCount = Environment.ProcessorCount;
            }

            if (string.IsNullOrEmpty(parameters["VegaAddress"]))
            {
                var serviceInfo = Helpers.GetVegaServiceInfo().Result;
                this.server = serviceInfo.Item1;
            }
            else
            {
                this.server = $"{parameters["VegaAddress"]}:{parameters["VegaPort"]}";
            }

            this.clients = Enumerable.Range(0, this.channelCount).Select(x => new RetriableRingMasterClient(
                s => new RingMasterClient(
                connectionString: s,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: requestTimeout,
                watcher: null),
                this.server))
                .ToArray();

            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            Helper.InitializeMdm(builder.Build(), this.ServiceContext.NodeContext.NodeName);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Starts the test job
        /// </summary>
        /// <param name="jobState">Job state object</param>
        /// <param name="cancellation">Cancellation token for cancelling the execution</param>
        /// <returns>async task</returns>
        public Task Start(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;

            try
            {
                this.TestPublishSubscribeAsync(jobState, cancellation).GetAwaiter().GetResult();
                jobState.Passed = true;
            }
            finally
            {
                foreach (var client in this.clients)
                {
                    client.Dispose();
                }
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// return the job metrics
        /// </summary>
        /// <returns>job metrics</returns>
        public Dictionary<string, double[]> GetJobMetrics()
        {
            return new Dictionary<string, double[]>
            {
                { "CreateLatency", this.CreateLatency.ToArray() },
                { "ReadLatency", this.ReadLatency.ToArray() },
                { "SetLatency", this.SetLatency.ToArray() },
                { "DeleteLatency", this.DeleteLatency.ToArray() },
                { "InstallWatcherLatency", this.InstallWatcherLatency.ToArray() },
            };
        }

        private async Task TestPublishSubscribeAsync(JobState jobState, CancellationToken cancellation)
        {
            var totalSizeMB = 0.5 * (this.maxDataSize + this.minDataSize) * this.partitionCount * this.nodeCountPerPartition / 1024 / 1024;
            this.log($"Creating {this.partitionCount} partitions, {this.nodeCountPerPartition} nodes in each partition, total amount of data {totalSizeMB} MB");

            var cancelShowProgress = new CancellationTokenSource();
            _ = this.ShowProgress(jobState, cancelShowProgress.Token);

            if (!await this.CreateNodeTree(cancellation).ConfigureAwait(false))
            {
                cancelShowProgress.Cancel();
                Assert.Fail($"No progress in CreateNodeTree after {this.requestTimeout} ms");
            }

            this.log("Reading all nodes...");
            if (!await this.ReadNodeTree(cancellation).ConfigureAwait(false))
            {
                cancelShowProgress.Cancel();
                Assert.Fail($"No progress in ReadNodeTree after {this.requestTimeout} ms");
            }

            cancelShowProgress.Cancel();

            var watchers = new ConcurrentBag<IWatcher>();
            int watcherTriggerCount = 0;
            long watcherDataDelivered = 0;
            var startTime = this.stopwatch.Elapsed;

            this.log($"Installing bulk watchers...");

            long watcherId = 0;
            var installTask = Helpers.ForEachAsync(
                Enumerable.Range(0, this.partitionCount),
                async (partitionIndex) =>
                {
                    foreach (var client in this.clients)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            break;
                        }

                        var path = $"{this.partitionKeyPrefix}{partitionIndex}";
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
                                    this.log($" -- {watchedEvent.EventType} / {watchedEvent.KeeperState} - {watchedEvent.Path}");
                                }
                            },
                            Id = (ulong)Interlocked.Increment(ref watcherId),
                        };
                        try
                        {
                            var operationStartTime = this.stopwatch.Elapsed;
                            await client.RegisterBulkWatcher(path, watcher).ConfigureAwait(false);
                            var operationDuration = this.stopwatch.Elapsed - operationStartTime;
                            MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, Test.Helpers.OperationType.InstallBulkWatcher);
                            this.InstallWatcherLatency.Add(operationDuration.TotalMilliseconds);

                            watchers.Add(watcher);
                        }
                        catch (Exception ex)
                        {
                            this.log($"  Watcher at path {path} failed to install: {ex.Message}");
                        }
                    }
                },
                this.threadCount);

            await Task.WhenAny(installTask, Task.Delay(this.requestTimeout)).ConfigureAwait(false);
            if (!installTask.IsCompleted && watcherTriggerCount == 0)
            {
                Assert.Fail($"No watcher event received after {this.requestTimeout} ms");
            }

            var duration = (this.stopwatch.Elapsed - startTime).TotalSeconds;
            var installRate = watchers.Count / duration;
            this.log($"Finished installing bulk watchers in {duration:F3} sec. Rate: {installRate:G4} /sec");
            MdmHelper.LogWatcherCountProcessed(watchers.Count, Test.Helpers.OperationType.InstallBulkWatcher);

            // Make some random change, one node in each partition, and wait for watcher being triggered
            for (int i = 0; i < this.testRepetitions && !cancellation.IsCancellationRequested; i++)
            {
                startTime = this.stopwatch.Elapsed;
                this.totalDataSize = 0;
                watcherTriggerCount = 0;
                watcherDataDelivered = 0;

                var unused1 = Task.Run(() => this.ChangeRandomNodeInTree());

                var timeoutClock = Stopwatch.StartNew();
                while (watcherTriggerCount < this.partitionCount * this.clients.Length
                    && timeoutClock.ElapsedMilliseconds < 30 * 1000
                    && !cancellation.IsCancellationRequested)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    Helper.LogAndSetJobStatus(this.log, jobState, $"Iteration {i} -- watcher event received: {watcherTriggerCount}, data received: {watcherDataDelivered}");
                }

                duration = (this.stopwatch.Elapsed - startTime).TotalSeconds;
                this.log($"Iteration {i} - {watcherTriggerCount} events / {watcherDataDelivered} bytes received in {duration} seconds. Read {this.totalDataSize} bytes.");
                MdmHelper.LogWatcherCountProcessed(watcherTriggerCount, Test.Helpers.OperationType.BulkWatcherTrigger);
                MdmHelper.LogOperationDuration((long)(duration * 1000), Test.Helpers.OperationType.BulkWatcherTrigger);
                MdmHelper.LogBytesProcessed(watcherDataDelivered, Test.Helpers.OperationType.BulkWatcherTrigger);
            }

            Assert.IsTrue(await this.DeleteNodeTree(cancellation).ConfigureAwait(false));
        }

        private async Task<bool> CreateNodeTree(CancellationToken cancellation)
        {
            this.totalDataCount = 0;
            this.totalDataSize = 0;
            var startTime = this.stopwatch.Elapsed;

            var createTask = Helpers.ForEachAsync(
                Enumerable.Range(0, this.partitionCount),
                async (partitionIndex) =>
                {
                    var rnd = new Random();

                    for (int i = 0; i < this.nodeCountPerPartition && !cancellation.IsCancellationRequested; i++)
                    {
                        var path = $"{this.partitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{i}";
                        var data = Helpers.MakeRandomData(rnd, rnd.Next(this.minDataSize, this.maxDataSize));

                        try
                        {
                            var operationStartTime = this.stopwatch.Elapsed;
                            await this.clients[this.partitionCount % this.channelCount].Create(path, data, null, CreateMode.PersistentAllowPathCreation, false).ConfigureAwait(false);
                            var operationDuration = this.stopwatch.Elapsed - operationStartTime;
                            MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, Test.Helpers.OperationType.BulkWatcherCreateNode);
                            this.CreateLatency.Add(operationDuration.TotalMilliseconds);

                            Interlocked.Add(ref this.totalDataSize, data.Length);
                            Interlocked.Increment(ref this.totalDataCount);
                        }
                        catch (Exception ex)
                        {
                            this.log($"Failed to create {path}: {ex.Message}");
                        }
                    }
                },
                this.threadCount);

            await Task.WhenAny(createTask, Task.Delay(this.requestTimeout)).ConfigureAwait(false);
            if (!createTask.IsCompleted && this.totalDataCount == 0)
            {
                // If no data is created successfully within timeout time, don't bother to read any more.
                return false;
            }
            else
            {
                // It is making progress. Wait until it's completed.
                await createTask.ConfigureAwait(false);
            }

            var duration = (this.stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = this.totalDataSize / duration;
            var qps = this.totalDataCount / duration;
            this.log($"{nameof(this.CreateNodeTree)}: {this.totalDataCount} nodes created, total data size is {this.totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(this.totalDataSize, Test.Helpers.OperationType.BulkWatcherCreateNode);

            return true;
        }

        private async Task<bool> ReadNodeTree(CancellationToken cancellation)
        {
            this.totalDataCount = 0;
            this.totalDataSize = 0;
            var startTime = this.stopwatch.Elapsed;

            var readTask = Helpers.ForEachAsync(
                Enumerable.Range(0, this.partitionCount),
                async (partitionIndex) =>
                {
                    for (int i = 0; i < this.nodeCountPerPartition && !cancellation.IsCancellationRequested; i++)
                    {
                        var path = $"{this.partitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{i}";

                        try
                        {
                            var operationStartTime = this.stopwatch.Elapsed;
                            var data = await this.clients[this.partitionCount % this.channelCount].GetData(path, null).ConfigureAwait(false);
                            var operationDuration = this.stopwatch.Elapsed - operationStartTime;
                            MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, Test.Helpers.OperationType.BulkWatcherReadNode);
                            this.ReadLatency.Add(operationDuration.TotalMilliseconds);

                            Interlocked.Add(ref this.totalDataSize, data.Length);
                            Interlocked.Increment(ref this.totalDataCount);
                        }
                        catch (Exception ex)
                        {
                            this.log($"Failed to read {path}: {ex.Message}");
                        }
                    }
                },
                this.threadCount);

            await Task.WhenAny(readTask, Task.Delay(this.requestTimeout)).ConfigureAwait(false);
            if (!readTask.IsCompleted && this.totalDataCount == 0)
            {
                // If no data is read successfully within timeout time, don't bother to read any more.
                return false;
            }
            else
            {
                // It is making progress. Wait until it's completed.
                await readTask.ConfigureAwait(false);
            }

            var duration = (this.stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = this.totalDataSize / duration;
            var qps = this.totalDataCount / duration;
            this.log($"{nameof(this.ReadNodeTree)}: {this.totalDataCount} nodes read, total data size is {this.totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(this.totalDataSize, Test.Helpers.OperationType.BulkWatcherReadNode);

            return true;
        }

        private async Task ChangeRandomNodeInTree()
        {
            this.totalDataCount = 0;
            this.totalDataSize = 0;
            var startTime = this.stopwatch.Elapsed;

            await Helpers.ForEachAsync(
                Enumerable.Range(0, this.partitionCount),
                async (partitionIndex) =>
                {
                    var rnd = new Random();

                    var index = rnd.Next(this.nodeCountPerPartition);
                    var path = $"{this.partitionKeyPrefix}{partitionIndex}/{RelativePathPrefix}/{index}";
                    var data = Helpers.MakeRandomData(rnd, rnd.Next(this.minDataSize, this.maxDataSize));

                    try
                    {
                        var operationStartTime = this.stopwatch.Elapsed;
                        await this.clients[partitionIndex % this.channelCount].SetData(path, data, -1).ConfigureAwait(false);
                        var operationDuration = this.stopwatch.Elapsed - operationStartTime;
                        MdmHelper.LogOperationDuration((long)operationDuration.TotalMilliseconds, Test.Helpers.OperationType.BulkWatcherChangeNode);
                        this.SetLatency.Add(operationDuration.TotalMilliseconds);

                        Interlocked.Add(ref this.totalDataSize, data.Length);
                        Interlocked.Increment(ref this.totalDataCount);
                    }
                    catch (Exception ex)
                    {
                        this.log($"Failed to set {path}: {ex.Message}");
                    }
                },
                this.threadCount)
                .ConfigureAwait(false);

            var duration = (this.stopwatch.Elapsed - startTime).TotalSeconds;
            var bps = this.totalDataSize / duration;
            var qps = this.totalDataCount / duration;
            this.log($"{nameof(this.ChangeRandomNodeInTree)}: {this.totalDataCount} nodes updated, total data size is {this.totalDataSize}. Rate: {bps:G4} byte/sec {qps:G4} /sec");
            MdmHelper.LogBytesProcessed(this.totalDataSize, Test.Helpers.OperationType.BulkWatcherChangeNode);
        }

        private async Task<bool> DeleteNodeTree(CancellationToken cancellation)
        {
            this.totalDataCount = 0;

            var deleteTask = Helpers.ForEachAsync(
                Enumerable.Range(0, this.partitionCount),
                async (partitionIndex) =>
                {
                    for (int i = 0; i < this.nodeCountPerPartition && !cancellation.IsCancellationRequested; i++)
                    {
                        var path = $"{this.partitionKeyPrefix}{partitionIndex}/";

                        try
                        {
                            var operationStartTime = this.stopwatch.Elapsed;
                            await this.clients[partitionIndex % this.channelCount].Delete(path, -1, DeleteMode.CascadeDelete).ConfigureAwait(false);
                            var operationDuration = this.stopwatch.Elapsed - operationStartTime;
                            this.DeleteLatency.Add(operationDuration.TotalMilliseconds);

                            Interlocked.Increment(ref this.totalDataCount);
                        }
                        catch (Exception ex)
                        {
                            this.log($"Failed to delete {path}: {ex}");
                        }
                    }
                },
                this.threadCount);

            await Task.WhenAny(deleteTask, Task.Delay(this.requestTimeout)).ConfigureAwait(false);
            if (!deleteTask.IsCompleted && this.totalDataCount == 0)
            {
                // If no data is deleted successfully within timeout time, don't bother to read any more.
                return false;
            }
            else
            {
                // It is making progress. Wait until it's completed.
                await deleteTask.ConfigureAwait(false);
            }

            this.log($"DeleteNodeTree is completed.");
            return true;
        }

        private async Task ShowProgress(JobState jobState, CancellationToken cancellation)
        {
            var lastCount = Interlocked.Read(ref this.totalDataCount);
            var lastSize = Interlocked.Read(ref this.totalDataSize);

            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(1000).ConfigureAwait(false);

                var count = Interlocked.Read(ref this.totalDataCount);
                var size = Interlocked.Read(ref this.totalDataSize);
                Helper.LogAndSetJobStatus(this.log, jobState, $"Count={count} +{count - lastCount} Size={size / 1024}k +{(size - lastSize) / 1024}k");

                lastCount = count;
                lastSize = size;
            }
        }
    }
}
