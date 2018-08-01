// <copyright file="RingMasterPerformance.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// RingMaster performance test
    /// </summary>
    public class RingMasterPerformance
    {
        private static IConfiguration appSettings;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private readonly Random random = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterPerformance"/> class.
        /// </summary>
        public RingMasterPerformance()
        {
        }

        /// <summary>
        /// Gets or sets the path to use for the test.
        /// </summary>
        public string TestPath { get; set; } = "/Performance";

        /// <summary>
        /// Gets or sets the timestream id that will be used by requests sent by this test.
        /// </summary>
        public ulong TimeStreamId { get; set; } = 0;

        /// <summary>
        /// Gets or sets maximum number of concurrent requests.
        /// </summary>
        public int MaxConcurrency { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum data size of created nodes.
        /// </summary>
        public int MaxDataSize { get; set; } = 16;

        /// <summary>
        /// Gets or sets the Minimum data size of created nodes.
        /// </summary>
        public int MinDataSize { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum number of children to create per node.
        /// </summary>
        public int MinChildrenPerNode { get; set; } = 32;

        /// <summary>
        /// Gets or sets the maximum number of children to create per node.
        /// </summary>
        public int MaxChildrenPerNode { get; set; } = 32;

        /// <summary>
        /// Gets or sets the number of requests that will be batched together.
        /// </summary>
        public int BatchLength { get; set; } = 32;

        /// <summary>
        /// Gets or sets the maximum number of nodes that the test will work with.
        /// </summary>
        public int MaxNodes { get; set; } = 100000;

        /// <summary>
        /// Gets or sets the maximum number of children to enumerate per get children request.
        /// </summary>
        public int MaxGetChildrenEnumerationCount { get; set; } = 256;

        /// <summary>
        /// Gets or sets the maximum allowed codepoint in generated random names.
        /// </summary>
        public int MaxAllowedCodePoint { get; set; } = 128;

        /// <summary>
        /// Gets or sets the maximum set operations.
        /// </summary>
        public int MaxSetOperations { get; set; } = 50000;

        /// <summary>
        /// Gets or sets the test maximum run time in seconds.
        /// </summary>
        public int TestMaxRunTimeInSeconds { get; set; } = 60;

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">Arguments provided to the program</param>
        public static void Main(string[] args)
        {
            string testType = "getdata";
            string ringMasterAddress = "127.0.0.1:99";
            string path = "/Performance";

            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(assemblyPath)).AddJsonFile("appSettings.json");
            appSettings = builder.Build();

            if (args.Length > 0)
            {
                testType = args[0].ToLower();
            }

            if (args.Length > 1)
            {
                ringMasterAddress = args[1];
            }

            if (args.Length > 2)
            {
                path = args[2];
            }

            bool useSecureConnection = false;
            X509Certificate[] clientCertificates = null;
            X509Certificate[] acceptedServerCertificates = null;

            if (bool.Parse(appSettings["SSL.UseSSL"]))
            {
                useSecureConnection = true;
                string[] clientCertificateThumbprints = appSettings["SSL.ClientCerts"].Split(new char[] { ';', ',' });
                clientCertificates = RingMasterClient.GetCertificatesFromThumbPrintOrFileName(clientCertificateThumbprints);
                acceptedServerCertificates = Certificates.GetDecodedCertificates(appSettings["SSL.ServerCerts"]);

                foreach (var certificate in clientCertificates)
                {
                    Trace.TraceInformation($"Client certificate: subject={certificate.Subject} thumbprint={certificate.GetCertHashString()}");
                }

                foreach (var certificate in acceptedServerCertificates)
                {
                    Trace.TraceInformation($"Server certificate: subject={certificate.Subject} thumbprint={certificate.GetCertHashString()}");
                }
            }
            else
            {
                Trace.TraceInformation("Not using SSL");
            }

            var performanceTest = new RingMasterPerformance();

            performanceTest.TestPath = path;
            performanceTest.TimeStreamId = ulong.Parse(appSettings["TimeStream"]);
            performanceTest.MaxConcurrency = int.Parse(appSettings["MaxConcurrency"]);
            performanceTest.MaxDataSize = int.Parse(appSettings["MaxDataSize"]);
            performanceTest.MinDataSize = int.Parse(appSettings["MinDataSize"]);
            performanceTest.MinChildrenPerNode = int.Parse(appSettings["MinChildrenPerNode"]);
            performanceTest.MaxChildrenPerNode = int.Parse(appSettings["MaxChildrenPerNode"]);
            performanceTest.BatchLength = int.Parse(appSettings["BatchLength"]);
            performanceTest.MaxAllowedCodePoint = int.Parse(appSettings["MaxAllowedCodePoint"]);
            performanceTest.MaxGetChildrenEnumerationCount = int.Parse(appSettings["MaxGetChildrenEnumerationCount"]);
            performanceTest.MaxSetOperations = int.Parse(appSettings["MaxSetOperations"]);
            performanceTest.MaxNodes = int.Parse(appSettings["MaxNodes"]);
            performanceTest.TestMaxRunTimeInSeconds = int.Parse(appSettings["TestMaxRunTimeInSeconds"]);

            int requestTimeout = int.Parse(appSettings["RequestTimeout"]);

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            var serverSpec = new RingMasterClient.ServerSpec
            {
                Endpoints = SecureTransport.ParseConnectionString(ringMasterAddress),
                UseSecureConnection = useSecureConnection,
            };

            if (useSecureConnection)
            {
                serverSpec.ClientCertificate = clientCertificates[0];
                serverSpec.AcceptedServerCertificates = acceptedServerCertificates;
                serverSpec.MustCheckCertificateRevocation = false;
                serverSpec.MustCheckCertificateTrustChain = false;
            }

            var clientConfiguration = new RingMasterClient.Configuration
            {
                DefaultTimeout = TimeSpan.FromMilliseconds(requestTimeout),
            };

            var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(performanceTest.TestMaxRunTimeInSeconds));

            try
            {
                using (var client = new RingMasterClient(serverSpec, clientConfiguration, instrumentation: null, watcher: null, cancellationToken: CancellationToken.None))
                using (var ringMaster = client.OpenTimeStream(performanceTest.TimeStreamId))
                {
                    if (testType == "setdata")
                    {
                        performanceTest.SetDataPerformanceTest(ringMaster).Wait();
                    }
                    else if (testType == "create")
                    {
                        int numNodes = 500000;

                        if (args.Length > 3)
                        {
                            numNodes = int.Parse(args[3]);
                        }

                        performanceTest.CreatePerformanceTest(ringMaster, numNodes);
                    }
                    else if (testType == "createflat")
                    {
                        int numNodes = 1000000;

                        if (args.Length > 3)
                        {
                            numNodes = int.Parse(args[3]);
                        }

                        performanceTest.CreateFlatPerformanceTest(ringMaster, numNodes);
                    }
                    else if (testType == "getchildren")
                    {
                        int maxChildren = 1000;

                        if (args.Length > 3)
                        {
                            maxChildren = int.Parse(args[3]);
                        }

                        performanceTest.GetChildrenPerformanceTest(ringMaster, maxChildren);
                    }
                    else if (testType == "delete")
                    {
                        performanceTest.DeletePerformanceTest(ringMaster).Wait();
                    }
                    else if (testType == "scheduleddelete")
                    {
                        performanceTest.ScheduledDeletePerformanceTest(ringMaster);
                    }
                    else if (testType == "connect")
                    {
                        int numConnections = 100;

                        if (args.Length > 3)
                        {
                            numConnections = int.Parse(args[3]);
                        }

                        var configuration = new SecureTransport.Configuration
                        {
                            UseSecureConnection = useSecureConnection,
                            ClientCertificates = clientCertificates,
                            ServerCertificates = acceptedServerCertificates,
                            CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                        };

                        IPEndPoint[] endpoints = SecureTransport.ParseConnectionString(ringMasterAddress);

                        performanceTest.ConnectPerformanceTest(configuration, endpoints, numConnections);
                    }
                    else if (testType == "exists")
                    {
                        performanceTest.ExistsPerformanceTest(ringMaster).Wait();
                    }
                    else if (testType == "watchers")
                    {
                        int maxWatchers = 1000;
                        if (args.Length > 3)
                        {
                            maxWatchers = int.Parse(args[3]);
                        }

                        performanceTest.WatcherPerformanceTest(ringMaster, maxWatchers, cancellation).Wait();
                    }
                    else
                    {
                        performanceTest.GetDataPerformanceTest(ringMaster, cancellation).Wait();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (AggregateException ex)
            {
                if (!ex.Flatten().InnerExceptions.Any(e => e is OperationCanceledException))
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Measures the performance of GetData requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>
        /// Task that tracks execution of this test
        /// </returns>
        private async Task GetDataPerformanceTest(IRingMasterRequestHandler ringMaster, CancellationTokenSource cancellation)
        {
            Trace.TraceInformation($"GetData performance test path={this.TestPath} batchLength={this.BatchLength}");

            var cancellationToken = cancellation.Token;
            var instrumentation = new GetDataPerformanceInstrumentation();
            var getDataPerformanceTest = new GetDataPerformance(instrumentation, this.MaxConcurrency, cancellationToken);

            await getDataPerformanceTest.LoadNodes(ringMaster, this.TestPath, this.MaxNodes, this.MaxGetChildrenEnumerationCount);

            var task = Task.Run(() => getDataPerformanceTest.QueueBatches(ringMaster, this.BatchLength));

            long lastSuccessCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"GetData success={instrumentation.Success} failure={instrumentation.Failure} rate={rate}");
                timer.Restart();
                lastSuccessCount = instrumentation.Success;
            }
        }

        /// <summary>
        /// Measures the performance of Exists requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <returns>Task that tracks execution of this test</returns>
        private async Task ExistsPerformanceTest(IRingMasterRequestHandler ringMaster)
        {
            Trace.TraceInformation($"Exists performance test path={this.TestPath} batchLength={this.BatchLength}");

            var cancellationToken = CancellationToken.None;
            var instrumentation = new ExistsPerformanceInstrumentation();
            var existsPerformanceTest = new ExistsPerformance(instrumentation, this.MaxConcurrency, CancellationToken.None);

            await existsPerformanceTest.LoadNodes(ringMaster, this.TestPath, this.MaxNodes, this.MaxGetChildrenEnumerationCount);

            var task = Task.Run(() => existsPerformanceTest.QueueBatches(ringMaster, this.BatchLength));

            long lastSuccessCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"Exists success={instrumentation.Success} failure={instrumentation.Failure} rate={rate}");
                timer.Restart();
                lastSuccessCount = instrumentation.Success;
            }
        }

        /// <summary>
        /// Measures the performance of Watchers.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="maxConcurrentWatchers">Maximum number of concurrent watchers</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>
        /// Task that tracks execution of this test
        /// </returns>
        private async Task WatcherPerformanceTest(IRingMasterRequestHandler ringMaster, int maxConcurrentWatchers, CancellationTokenSource cancellation)
        {
            Trace.TraceInformation($"Watcher performance test path={this.TestPath} batchLength={this.BatchLength}");

            var cancellationToken = cancellation.Token;
            var instrumentation = new WatcherPerformanceInstrumentation();
            var watcherPerformanceTest = new WatcherPerformance(instrumentation, cancellationToken);

            await watcherPerformanceTest.LoadNodes(ringMaster, this.TestPath, this.MaxNodes, this.MaxGetChildrenEnumerationCount);

            var task = Task.Run(() => watcherPerformanceTest.SetWatchers(ringMaster, maxConcurrentWatchers));

            long lastNotificationCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Notifications - lastNotificationCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"Watchers success={instrumentation.Success} failure={instrumentation.Failure} notifications={instrumentation.Notifications} notificationRate={rate}");
                timer.Restart();
                lastNotificationCount = instrumentation.Notifications;
            }
        }

        /// <summary>
        /// Measures the performance of SetData requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <returns>Task that tracks execution of this test</returns>
        private async Task SetDataPerformanceTest(IRingMasterRequestHandler ringMaster)
        {
            Trace.TraceInformation($"SetData performance test path={this.TestPath}");

            var cancellationToken = CancellationToken.None;
            var instrumentation = new SetDataPerformanceInstrumentation();
            var setDataPerformanceTest = new SetDataPerformance(instrumentation, this.MaxConcurrency, CancellationToken.None);

            await setDataPerformanceTest.LoadNodes(ringMaster, this.TestPath, this.MaxNodes, this.MaxGetChildrenEnumerationCount);

            var task = Task.Run(() => setDataPerformanceTest.QueueRequests(ringMaster, this.BatchLength, this.MaxSetOperations));

            long lastSuccessCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"SetData success={instrumentation.Success}, failure={instrumentation.Failure}, rate={rate}");
                timer.Restart();
                lastSuccessCount = instrumentation.Success;
            }
        }

        /// <summary>
        /// Measures the performance of create requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="maxNodes">Number of nodes to create</param>
        private void CreatePerformanceTest(IRingMasterRequestHandler ringMaster, int maxNodes)
        {
            var instrumentation = new CreatePerformanceInstrumentation();
            var createPerformanceTest = new CreatePerformance(instrumentation, this.MaxConcurrency, CancellationToken.None);
            createPerformanceTest.MinChildrenCountPerNode = this.MinChildrenPerNode;
            createPerformanceTest.MaxChildrenCountPerNode = this.MaxChildrenPerNode;
            createPerformanceTest.MaxDataSizePerNode = this.MaxDataSize;
            createPerformanceTest.MinDataSizePerNode = this.MinDataSize;
            createPerformanceTest.MaxAllowedCodePoint = this.MaxAllowedCodePoint;

            Trace.TraceInformation($"Create performance test path={this.TestPath}, maxNumberOfNodes={maxNodes}");
            var task = Task.Run(() => createPerformanceTest.CreateHierarchy(ringMaster, this.TestPath, this.BatchLength, maxNodes));

            long lastSuccessCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"Create success={instrumentation.Success}, failure={instrumentation.Failure}, rate={rate}");
                timer.Restart();
                lastSuccessCount = instrumentation.Success;
            }
        }

        /// <summary>
        /// Measures the performance of create requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="maxNodes">Number of nodes to create</param>
        private void CreateFlatPerformanceTest(IRingMasterRequestHandler ringMaster, int maxNodes)
        {
            var instrumentation = new CreatePerformanceInstrumentation();
            var createPerformanceTest = new CreatePerformance(instrumentation, this.MaxConcurrency, CancellationToken.None);
            createPerformanceTest.MaxDataSizePerNode = this.MaxDataSize;
            createPerformanceTest.MaxAllowedCodePoint = this.MaxAllowedCodePoint;

            Trace.TraceInformation($"CreateFlat performance test path={this.TestPath}, maxNumberOfNodes={maxNodes}");
            var task = Task.Run(() => createPerformanceTest.CreateFlat(ringMaster, this.TestPath, this.BatchLength, maxNodes));

            long lastSuccessCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"Create success={instrumentation.Success}, failure={instrumentation.Failure}, rate={rate}");
                timer.Restart();
                lastSuccessCount = instrumentation.Success;
            }
        }

        /// <summary>
        /// Measures the performance of delete requests.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <returns>Task that tracks execution of this test</returns>
        private async Task DeletePerformanceTest(IRingMasterRequestHandler ringMaster)
        {
            var instrumentation = new DeletePerformanceInstrumentation();
            var deletePerformanceTest = new DeletePerformance(instrumentation, this.MaxConcurrency, CancellationToken.None);

            Trace.TraceInformation($"Delete performance test path={this.TestPath}");

            await deletePerformanceTest.LoadNodes(ringMaster, this.TestPath, this.MaxNodes, this.MaxGetChildrenEnumerationCount);

            var task = Task.Run(() => deletePerformanceTest.QueueDeletes(ringMaster, this.BatchLength));

            long lastSuccessCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"Delete success={instrumentation.Success}, failure={instrumentation.Failure}, rate={rate}");
                timer.Restart();
                lastSuccessCount = instrumentation.Success;
            }
        }

        /// <summary>
        /// Measures the performance of scheduled delete operation.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        private void ScheduledDeletePerformanceTest(IRingMasterRequestHandler ringMaster)
        {
            var instrumentation = new DeletePerformanceInstrumentation();
            var deletePerformanceTest = new DeletePerformance(instrumentation, this.MaxConcurrency, CancellationToken.None);

            Trace.TraceInformation($"Scheduled Delete performance test path={this.TestPath}");

            var task = Task.Run(() => deletePerformanceTest.ScheduledDelete(ringMaster, this.TestPath));

            var totalTime = Stopwatch.StartNew();
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                Trace.TraceInformation($"Scheduled Delete ElapsedMilliseconds={totalTime.ElapsedMilliseconds}");
                timer.Restart();
            }
        }

        private void GetChildrenPerformanceTest(IRingMasterRequestHandler ringMaster, int maxChildren)
        {
            var instrumentation = new GetChildrenPerformanceInstrumentation();
            var getChildrenPerformanceTest = new GetChildrenPerformance(instrumentation, this.MaxConcurrency, CancellationToken.None);

            Trace.TraceInformation($"GetChildren performance test path={this.TestPath}, maxChildren={maxChildren}");
            var task = Task.Run(() => getChildrenPerformanceTest.QueueRequests(ringMaster, this.TestPath, maxChildren));

            long lastSuccessCount = 0;
            var timer = Stopwatch.StartNew();
            while (!task.Wait(5000))
            {
                timer.Stop();
                long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                Trace.TraceInformation($"GetChildren success={instrumentation.Success}, failure={instrumentation.Failure}, rate={rate}");
                timer.Restart();
                lastSuccessCount = instrumentation.Success;
            }
        }

        private void ConnectPerformanceTest(SecureTransport.Configuration configuration, IPEndPoint[] endpoints, int numConnections)
        {
            var instrumentation = new ConnectPerformanceInstrumentation();
            var random = new Random();

            int minConnectionLifetimeSeconds = int.Parse(appSettings["ConnectPerformance.MinConnectionLifetimeSeconds"]);
            int maxConnectionLifetimeSeconds = int.Parse(appSettings["ConnectPerformance.MaxConnectionLifetimeSeconds"]);

            Func<IRingMasterRequestHandler> createConnection = () =>
            {
                var connectionConfiguration = new SecureTransport.Configuration
                {
                    UseSecureConnection = configuration.UseSecureConnection,
                    ClientCertificates = configuration.ClientCertificates,
                    ServerCertificates = configuration.ServerCertificates,
                    CommunicationProtocolVersion = configuration.CommunicationProtocolVersion,
                    MaxConnectionLifespan = TimeSpan.FromSeconds(random.Next(minConnectionLifetimeSeconds, maxConnectionLifetimeSeconds)),
                };

                var protocol = new RingMasterCommunicationProtocol();
                var transport = new SecureTransport(connectionConfiguration, instrumentation, CancellationToken.None);
                var client = new RingMasterClient(protocol, transport);
                transport.StartClient(endpoints);

                client.Exists("/", watcher: null).Wait();
                return (IRingMasterRequestHandler)client;
            };

            using (var connectPerformanceTest = new ConnectPerformance(instrumentation, this.MaxConcurrency, CancellationToken.None))
            {
                Trace.TraceInformation($"Connect performance test numConnections={numConnections}, path={this.TestPath}, minConnectionLifetimeSeconds={minConnectionLifetimeSeconds}, maxConnectionLifetimeSeconds={maxConnectionLifetimeSeconds}");

                connectPerformanceTest.EstablishConnections(createConnection, numConnections);

                var task = Task.Run(() => connectPerformanceTest.QueueRequests(this.TestPath));

                long lastSuccessCount = 0;
                var timer = Stopwatch.StartNew();
                while (!task.Wait(5000))
                {
                    timer.Stop();
                    long rate = (long)((instrumentation.Success - lastSuccessCount) * 1000) / timer.ElapsedMilliseconds;
                    Trace.TraceInformation($"Connections created={instrumentation.ConnectionCreatedCount}, closed={instrumentation.ConnectionClosedCount}, requestSuccess={instrumentation.Success}, requestFailure={instrumentation.Failure}, rate={rate}");
                    timer.Restart();
                    lastSuccessCount = instrumentation.Success;
                }
            }
        }

        private class GetDataPerformanceInstrumentation : GetDataPerformance.IInstrumentation
        {
            public long Success { get; private set; }

            public long Failure { get; private set; }

            public void BatchFailed(int batchLength)
            {
            }

            public void BatchProcessed(TimeSpan latency, int batchLength, int successCount, int failureCount)
            {
                lock (this)
                {
                    this.Success += successCount;
                    this.Failure += failureCount;
                }
            }

            public void NodeLoaded(int nodeCount)
            {
            }
        }

        private class ExistsPerformanceInstrumentation : ExistsPerformance.IInstrumentation
        {
            public long Success { get; private set; }

            public long Failure { get; private set; }

            public void BatchFailed(int batchLength)
            {
            }

            public void BatchProcessed(TimeSpan latency, int batchLength, int successCount, int failureCount)
            {
                lock (this)
                {
                    this.Success += successCount;
                    this.Failure += failureCount;
                }
            }

            public void NodeLoaded(int nodeCount)
            {
            }
        }

        private class WatcherPerformanceInstrumentation : WatcherPerformance.IInstrumentation
        {
            private long succeeded = 0;
            private long failed = 0;
            private long notified = 0;

            public long Success => this.succeeded;

            public long Failure => this.failed;

            public long Notifications => this.notified;

            public void NodeLoaded(int nodeCount)
            {
            }

            public void SetWatcherSucceeded(TimeSpan latency)
            {
                Interlocked.Increment(ref this.succeeded);
            }

            public void SetWatcherFailed()
            {
                Interlocked.Increment(ref this.failed);
            }

            public void WatcherNotified(TimeSpan watchDuration)
            {
                Interlocked.Increment(ref this.notified);
            }
        }

        private class SetDataPerformanceInstrumentation : SetDataPerformance.IInstrumentation
        {
            public long Success { get; private set; }

            public long Failure { get; private set; }

            public void NodeLoaded(int nodeCount)
            {
            }

            public void SetDataMultiFailed(int failureCount)
            {
                lock (this)
                {
                    this.Failure += failureCount;
                }
            }

            public void SetDataMultiSucceeded(int successCount, TimeSpan latency)
            {
                lock (this)
                {
                    this.Success += successCount;
                }
            }
        }

        private class CreatePerformanceInstrumentation : CreatePerformance.IInstrumentation
        {
            public long Success { get; private set; }

            public long Failure { get; private set; }

            public void CreateMultiSucceeded(int successCount, TimeSpan elapsed)
            {
                lock (this)
                {
                    this.Success += successCount;
                }
            }

            public void CreateMultiFailed(int failureCount)
            {
                lock (this)
                {
                    this.Failure += failureCount;
                }
            }

            public void NodeQueuedForCreate(int nodeCount)
            {
            }
        }

        private class DeletePerformanceInstrumentation : DeletePerformance.IInstrumentation
        {
            public long Success { get; private set; }

            public long Failure { get; private set; }

            public void DeleteMultiSucceeded(int successCount, TimeSpan elapsed)
            {
                lock (this)
                {
                    this.Success += successCount;
                }
            }

            public void DeleteMultiFailed(int failureCount)
            {
                lock (this)
                {
                    this.Failure += failureCount;
                }
            }

            public void NodeLoaded(int nodeCount)
            {
            }

            public void NodeQueuedForDelete(int nodeCount)
            {
            }
        }

        private class GetChildrenPerformanceInstrumentation : GetChildrenPerformance.IInstrumentation
        {
            public long Success { get; private set; }

            public long Failure { get; private set; }

            public void GetChildrenSucceeded(string nodePath, int childrenCount, TimeSpan elapsed)
            {
                lock (this)
                {
                    this.Success++;
                }
            }

            public void GetChildrenFailed(string nodePath)
            {
                lock (this)
                {
                    this.Failure++;
                }
            }
        }

        private class ConnectPerformanceInstrumentation : ConnectPerformance.IInstrumentation, ISecureTransportInstrumentation
        {
            public long ConnectionCreatedCount { get; private set; }

            public long ConnectionClosedCount { get; private set; }

            public long Success { get; private set; }

            public long Failure { get; private set; }

            public void ConnectionCreated(int connectionCount, TimeSpan elapsed)
            {
            }

            public void RequestFailed()
            {
                lock (this)
                {
                    this.Failure++;
                }
            }

            public void RequestSucceeded(TimeSpan elapsed)
            {
                lock (this)
                {
                    this.Success++;
                }
            }

            public void ConnectionEstablished(IPEndPoint serverEndPoint, string serverIdentity, TimeSpan setupTime)
            {
            }

            public void EstablishConnectionFailed(TimeSpan processingTime)
            {
            }

            public void ConnectionAccepted(IPEndPoint clientEndPoint, string clientIdentity, TimeSpan setupTime)
            {
            }

            public void ConnectionCreated(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity)
            {
                lock (this)
                {
                    this.ConnectionCreatedCount++;
                }
            }

            public void ConnectionClosed(long connectionId, IPEndPoint remoteEndPoint, string remoteIdentity)
            {
                lock (this)
                {
                    this.ConnectionClosedCount++;
                }
            }

            public void AcceptConnectionFailed(IPEndPoint clientEndPoint, TimeSpan processingTime)
            {
            }
        }
    }
}
