// <copyright file="TestBase.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DistTestCommonProto;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Vega.Test.Helpers;

    /// <summary>
    /// Test create/get/update/delete tree
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.ITestJob" />
    internal abstract class TestBase : ITestJob
    {
        /// <summary>
        /// The maximum children count
        /// </summary>
        protected const int MaxChildrenCount = 256;

        /// <summary>
        /// Total size of the data.
        /// </summary>
        private long totalDataSize;

        /// <summary>
        /// Total number of data items being processed
        /// </summary>
        private long totalDataCount;

        /// <summary>
        /// The total number of failures in each test
        /// </summary>
        private long totalFailures;

        /// <summary>
        /// The processed data counts
        /// </summary>
        private ConcurrentBag<double> processedDataCounts = new ConcurrentBag<double>();

        /// <summary>
        /// RingMaster clients, i.e. connections to the backend
        /// </summary>
        private IRingMasterRequestHandler[] clients;

        /// <summary>
        /// Request timeout to the backend
        /// </summary>
        private int requestTimeout = 100000;

        /// <summary>
        /// Endpoint of the backend
        /// </summary>
        private string server;

        /// <summary>
        /// Number of thread to send request in parallel
        /// </summary>
        private int threadCount = -1;

        /// <summary>
        /// The watcher trigger count
        /// </summary>
        private long watcherTriggerCount = 0;

        /// <summary>
        /// The bulk watcher clients
        /// </summary>
        private IRingMasterRequestHandler[] bulkWatcherClients;

        /// <summary>
        /// Gets the helper client
        /// </summary>
        protected IRingMasterRequestHandler HelperClient { get; private set; }

        /// <summary>
        /// Gets the log.
        /// </summary>
        protected Action<string> Log { get; private set; }

        /// <summary>
        /// Gets the name of the root node. If multiple instances of perf test are running, each one can choose a different
        /// name to stress the service in parallel.
        /// </summary>
        protected string RootNodeName { get; private set; }

        /// <summary>
        /// Gets the minimum data payload size
        /// </summary>
        protected int MinDataSize { get; private set; }

        /// <summary>
        /// Gets the maximum data payload size
        /// </summary>
        protected int MaxDataSize { get; private set; }

        /// <summary>
        /// Gets the number of seconds each test should run
        /// </summary>
        protected int TestCaseSeconds { get; private set; }

        /// <summary>
        /// Gets the number of batched operation in a group
        /// </summary>
        protected int BatchOpCount { get; private set; }

        /// <summary>
        /// Gets the number of async task to await in a batch
        /// </summary>
        protected int AsyncTaskCount { get; private set; }

        /// <summary>
        /// Gets the large tree ratio.
        /// The create test will create a large number of small trees (child number 0 - 20)
        /// and a small number (20 - 50) large trees, which have a lot more children.
        /// Each thread in the test switches between creating small trees and large trees continuously.
        /// So this magic number actually means, after creating one small tree,
        /// how many large tree nodes should it create.
        /// </summary>
        protected int LargeTreeRatio { get; private set; }

        /// <summary>
        /// Gets or sets the large tree roots
        /// </summary>
        protected List<Guid> LargeTreeRoots { get; set; }

        /// <summary>
        /// Gets the queued nodes.
        /// this is used in the producer/consumer model,
        /// stores the nodes that have been traversed by the helper client
        /// and yet to be visit by the consumers (get/set/delete).
        /// </summary>
        protected ConcurrentQueue<string> QueuedNodes { get; } = new ConcurrentQueue<string>();

        /// <summary>
        /// Gets the service context.
        /// </summary>
        protected StatelessServiceContext ServiceContext { get; private set; }

        /// <summary>
        /// Gets the watch count per node.
        /// </summary>
        protected int WatcherCountPerNode { get; private set; }

        /// <summary>
        /// Initializes the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="serviceContext">service context</param>
        /// <returns>async task</returns>
        public Task Initialize(Dictionary<string, string> parameters, StatelessServiceContext serviceContext)
        {
            this.Log = s => VegaDistTestEventSource.Log.GeneralMessage(s);

            this.ServiceContext = serviceContext;
            this.RootNodeName = parameters["RootNodeName"];
            this.MinDataSize = int.Parse(parameters["MinDataSize"]);
            this.MaxDataSize = int.Parse(parameters["MaxDataSize"]);
            this.BatchOpCount = int.Parse(parameters["BatchOpCount"]);
            this.TestCaseSeconds = int.Parse(parameters["TestCaseSeconds"]);
            this.requestTimeout = int.Parse(parameters["RequestTimeout"]);
            this.threadCount = int.Parse(parameters["ThreadCount"]);
            this.AsyncTaskCount = int.Parse(parameters["AsyncTaskCount"]);
            this.LargeTreeRatio = int.Parse(parameters["LargeTreeRatio"]);
            this.WatcherCountPerNode = int.Parse(parameters["WatcherCountPerNode"]);

            if (string.IsNullOrEmpty(parameters["VegaAddress"]))
            {
                var serviceInfo = Helpers.GetVegaServiceInfo().Result;
                this.server = serviceInfo.Item1;
            }
            else
            {
                this.server = $"{parameters["VegaAddress"]}:{parameters["VegaPort"]}";
            }

            if (this.WatcherCountPerNode > 0)
            {
                this.bulkWatcherClients = Enumerable.Range(0, this.WatcherCountPerNode).Select(x => new RetriableRingMasterClient(
                    s => new RingMasterClient(
                    connectionString: s,
                    clientCerts: null,
                    serverCerts: null,
                    requestTimeout: this.requestTimeout,
                    watcher: null),
                    this.server))
                    .ToArray();
            }

            if (this.threadCount < 0)
            {
                this.threadCount = Environment.ProcessorCount;
            }

            this.clients = Enumerable.Range(0, this.threadCount).Select(x => new RetriableRingMasterClient(
                s => new RingMasterClient(
                connectionString: s,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: this.requestTimeout,
                watcher: null),
                this.server))
                .ToArray();

            this.HelperClient = new RetriableRingMasterClient(
                s => new RingMasterClient(
                connectionString: s,
                clientCerts: null,
                serverCerts: null,
                requestTimeout: this.requestTimeout,
                watcher: null),
                this.server);

            // Ensure the sufficient threads in the pool for the async tasks.
            ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
            ThreadPool.SetMinThreads(this.threadCount * 2, minIOC);

            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            Helper.InitializeMdm(builder.Build(), this.ServiceContext.NodeContext.NodeName);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Starts the specified test.
        /// </summary>
        /// <param name="jobState">State of the job.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>async task</returns>
        public Task Start(JobState jobState, CancellationToken cancellation)
        {
            try
            {
                this.RunTest(jobState, cancellation);
            }
            catch (Exception ex)
            {
                jobState.Passed = false;
                Helper.LogAndSetJobStatus(this.Log, jobState, $"Failed running test. Exception: {ex.ToString()}");
            }
            finally
            {
                this.TestCleanup();
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Gets the job metrics.
        /// </summary>
        /// <returns>job metrics</returns>
        public Dictionary<string, double[]> GetJobMetrics()
        {
            return new Dictionary<string, double[]>
            {
                { "ProcessedCounts", this.processedDataCounts.ToArray() },
            };
        }

        /// <summary>
        /// Runs the test.
        /// </summary>
        /// <param name="jobState">State of the job.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>async task</returns>
        protected abstract Task RunTest(JobState jobState, CancellationToken cancellation);

        /// <summary>
        /// Main test workflow
        /// </summary>
        /// <param name="testTitle">Title of the test case</param>
        /// <param name="operationType">the operation type</param>
        /// <param name="workload">Workload in each thread</param>
        /// <param name="jobState">the job state</param>
        /// <param name="durationInSeconds">How long the test should run</param>
        /// <returns>Number of operations per second</returns>
        protected async Task<double> TestFlowAsync(
            string testTitle,
            Test.Helpers.OperationType operationType,
            Func<IRingMasterRequestHandler, CancellationToken, int, Task> workload,
            JobState jobState,
            int durationInSeconds)
        {
            this.ResetCounts();

            var cancellation = new CancellationTokenSource();

            this.Log($"Starting test {testTitle} in {this.threadCount} threads");

            var lastCount = Interlocked.Read(ref this.totalDataCount);
            var lastSize = Interlocked.Read(ref this.totalDataSize);

            var threads = Helpers.StartMultipleThreads(
                this.threadCount,
                (object n) => workload(this.clients[(int)n], cancellation.Token, (int)n).GetAwaiter().GetResult());

            var initialCount = lastCount;
            var initialSize = lastSize;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < durationInSeconds; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                long size = Interlocked.Read(ref this.totalDataSize);
                long delta = size - lastSize;

                long count = Interlocked.Read(ref this.totalDataCount);
                long deltaCount = count - lastCount;

                this.Log($"{DateTime.Now} - {deltaCount} - {delta}");
                jobState.Status = $"processed data count: {count}, failures: {Interlocked.Read(ref this.totalFailures)}. Queued node count: {this.QueuedNodes.Count()}";
                this.processedDataCounts.Add(deltaCount);

                lastSize = size;
                lastCount = count;
            }

            stopwatch.Stop();
            var processedCount = Interlocked.Read(ref this.totalDataCount) - initialCount;
            var processedSize = Interlocked.Read(ref this.totalDataSize) - initialSize;
            var rate = processedCount / stopwatch.Elapsed.TotalSeconds;

            Helper.LogAndSetJobStatus(this.Log, jobState, $"Stopping test {testTitle}. Data processed {processedSize} bytes in {processedCount} ops. rate: {rate:G4} /sec, Failures = {this.totalFailures}");
            MdmHelper.LogBytesProcessed(processedSize, operationType);

            cancellation.Cancel();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            this.Log($"Stopped {testTitle}.");

            return rate;
        }

        /// <summary>
        /// Tests create scenario using either batch or multi
        /// </summary>
        /// <param name="batch">true if using batch, false if using multi</param>
        /// <param name="operationType">Type of the operation.</param>
        /// <param name="jobState">the job state</param>
        /// <returns>
        /// Request per second
        /// </returns>
        protected async Task<double> TestBatchOrMultiCreate(bool batch, Test.Helpers.OperationType operationType, JobState jobState)
        {
            var name = batch ? "Batch" : "Multi";
            Random rnd = new Random();

            // number of large trees will be a random number between (20, 50)
            this.LargeTreeRoots = Enumerable.Range(0, rnd.Next(20, 50)).Select(x => Guid.NewGuid()).ToList();

            return await this.TestFlowAsync(
                $"{name}(Create) node perf test",
                operationType,
                async (client, cancellationToken, threadId) =>
                {
                    var clock = Stopwatch.StartNew();
                    var rootName = $"{this.RootNodeName}_{name}";
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var ops = new List<Op>(this.BatchOpCount);
                        var totalSize = 0;

                        int smallTreeNodeCount = rnd.Next(this.BatchOpCount / 2);
                        int bigTreeNodeCount = this.BatchOpCount - smallTreeNodeCount;

                        var smallSubtreeId = Guid.NewGuid();
                        while (smallTreeNodeCount-- > 0)
                        {
                            var data = Helpers.MakeRandomData(rnd, rnd.Next(this.MinDataSize, this.MaxDataSize));
                            totalSize += data.Length;
                            ops.Add(Op.Create($"/{rootName}/vnet{smallSubtreeId}/mappings/v4ca/{Guid.NewGuid()}", data, null, CreateMode.PersistentAllowPathCreation));
                        }

                        while (bigTreeNodeCount-- > 0)
                        {
                            var data = Helpers.MakeRandomData(rnd, rnd.Next(this.MinDataSize, this.MaxDataSize));
                            totalSize += data.Length;
                            int idx = rnd.Next(this.LargeTreeRoots.Count);
                            ops.Add(Op.Create($"/{rootName}/vnet{this.LargeTreeRoots[idx]}/mappings/v4ca/{Guid.NewGuid()}", data, null, CreateMode.PersistentAllowPathCreation));
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

                            this.AddTotalDataSize(totalSize);
                            this.IncrementTotalDataCount(ops.Count);
                        }
                        catch (Exception ex)
                        {
                            this.IncrementTotalFailures();
                            this.Log($"Failed to call {name}: {ex.Message}");
                        }
                    }
                },
                jobState,
                this.TestCaseSeconds);
        }

        /// <summary>
        /// Traverses the tree.
        /// </summary>
        /// <param name="treeRoots">The tree roots.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>async task</returns>
        protected async Task TraverseTree(List<string> treeRoots, CancellationToken cancellation)
        {
            var allRoots = await this.HelperClient.GetChildren("/", null);
            treeRoots.RemoveAll(r => !allRoots.Contains(r));

            int idx = 0;
            while (idx < treeRoots.Count && !cancellation.IsCancellationRequested)
            {
                var rootName = treeRoots[idx++];

                this.Log("start traversing " + rootName);
                var startFrom = string.Empty;
                while (!cancellation.IsCancellationRequested)
                {
                    var children = await this.HelperClient.GetChildren($"/{rootName}/Instance{this.ServiceContext.ReplicaOrInstanceId}", null, $">:{MaxChildrenCount}:{startFrom}");

                    foreach (var child in children)
                    {
                        var subroot = $"/{rootName}/Instance{this.ServiceContext.ReplicaOrInstanceId}/{child}/mappings/v4ca";
                        var subtreeStartFrom = string.Empty;
                        while (!cancellation.IsCancellationRequested)
                        {
                            IReadOnlyList<string> leafChildren = new List<string>();
                            try
                            {
                                leafChildren = await this.HelperClient.GetChildren(subroot, null, $">:{MaxChildrenCount}:{subtreeStartFrom}");
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
                                this.QueuedNodes.Enqueue($"{subroot}/{leaf}");
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

                this.Log("finished traversing " + rootName);
            }
        }

        /// <summary>
        /// Installs the bulk watcher.
        /// </summary>
        /// <param name="watchedEventType">the watched event type</param>
        /// <param name="jobState">the job state</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>async task</returns>
        protected async Task InstallBulkWatcher(WatchedEvent.WatchedEventType watchedEventType, JobState jobState, CancellationToken cancellation)
        {
            if (this.WatcherCountPerNode <= 0)
            {
                return;
            }

            this.Log($"installing bulk watchers. Number per node: {this.WatcherCountPerNode}");

            this.watcherTriggerCount = 0;
            var watcher = new CallbackWatcher
            {
                OnProcess = (watchedEvent) =>
                {
                    if (watchedEvent.EventType == watchedEventType)
                    {
                        Interlocked.Increment(ref this.watcherTriggerCount);
                    }
                },
            };

            var startFrom = string.Empty;

            var clock = Stopwatch.StartNew();
            long processedCount = 0;
            while (!cancellation.IsCancellationRequested)
            {
                var children = await this.HelperClient.GetChildren($"/{this.RootNodeName}/Instance{this.ServiceContext.ReplicaOrInstanceId}", null, $">:{MaxChildrenCount}:{startFrom}");
                await Helpers.ForEachAsync(
                    children,
                    async (child) =>
                    {
                        var subroot = $"/{this.RootNodeName}/Instance{this.ServiceContext.ReplicaOrInstanceId}/{child}";

                        List<Task> addWatcherTasks = new List<Task>();
                        foreach (var watcherClient in this.bulkWatcherClients)
                        {
                            addWatcherTasks.Add(Task.Run(async () =>
                            {
                                await watcherClient.RegisterBulkWatcher(subroot, watcher);
                            }));
                        }

                        await Task.WhenAll(addWatcherTasks);
                        startFrom = child;
                    },
                    children.Count);

                processedCount += children.Count;
                Helper.LogAndSetJobStatus(
                    this.Log,
                    jobState,
                    $"Installing bulk watcher. {processedCount} number of nodes processed. Rate: {processedCount * this.clients.Count() / clock.Elapsed.TotalSeconds:G4} /sec");

                if (children.Count < MaxChildrenCount)
                {
                    break;
                }
            }

            this.Log("finished installing bulk watchers");
        }

        /// <summary>
        /// Waits for watchers.
        /// </summary>
        /// <returns>async task</returns>
        protected async Task<bool> WaitForWatchers()
        {
            var timeoutClock = Stopwatch.StartNew();
            var expectedTriggerCount = this.totalDataCount * this.WatcherCountPerNode;
            do
            {
                await Task.Delay(1000);
                this.Log($"waiting for notification. watcher triggered count:  {this.watcherTriggerCount}, expected count: {expectedTriggerCount}. Failures: {this.totalFailures}");
            }
            while (this.watcherTriggerCount < expectedTriggerCount && timeoutClock.ElapsedMilliseconds < 5000);

            return this.watcherTriggerCount == expectedTriggerCount;
        }

        /// <summary>
        /// Increments the total data count.
        /// </summary>
        protected void IncrementTotalDataCount()
        {
            Interlocked.Increment(ref this.totalDataCount);
        }

        /// <summary>
        /// Increments the total data count.
        /// </summary>
        /// <param name="count">The count.</param>
        protected void IncrementTotalDataCount(int count)
        {
            Interlocked.Add(ref this.totalDataCount, count);
        }

        /// <summary>
        /// Increments the total failures.
        /// </summary>
        protected void IncrementTotalFailures()
        {
            Interlocked.Increment(ref this.totalFailures);
        }

        /// <summary>
        /// Adds the total size of the data.
        /// </summary>
        /// <param name="size">The size.</param>
        protected void AddTotalDataSize(int size)
        {
            Interlocked.Add(ref this.totalDataSize, size);
        }

        private void ResetCounts()
        {
            this.totalDataCount = this.totalDataSize = this.totalFailures = 0;
        }

        private void TestCleanup()
        {
            foreach (var client in this.clients)
            {
                client.Dispose();
            }

            if (this.bulkWatcherClients != null)
            {
                foreach (var client in this.bulkWatcherClients)
                {
                    client.Dispose();
                }
            }

            this.HelperClient.Dispose();
        }
    }
}
