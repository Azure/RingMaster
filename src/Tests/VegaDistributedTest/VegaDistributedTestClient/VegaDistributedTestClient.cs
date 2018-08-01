// <copyright file="VegaDistributedTestClient.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using DistTestCommonProto;
    using Grpc.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Vega.DistributedJobControllerProto;
    using Microsoft.Vega.Test.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Client program to communicate with the distributed job controller, start test, and collect result
    /// </summary>
    [TestClass]
    public sealed class VegaDistributedTestClient : IDisposable
    {
        /// <summary>
        /// IP address or the host name of the server
        /// </summary>
        private static string serverAddress;

        /// <summary>
        /// Global log function
        /// </summary>
        private static Action<string> log = Console.WriteLine;

        /// <summary>
        /// Parameters passed by TAEF in the form of "TE.exe VegaDistributedTestClient /p:Key=Value"
        /// </summary>
        private static IDictionary<string, object> testProperties;

        /// <summary>
        /// The root node name
        /// </summary>
        private static string rootNodeName = "DistPerf";

        /// <summary>
        /// IP address or the host name of the Vega service
        /// </summary>
        private static string vegaAddress;

        /// <summary>
        /// The vega port number
        /// </summary>
        private static string vegaPortNumber;

        /// <summary>
        /// Name or regex of the node which initiates the primary failover
        /// </summary>
        private static string killerNodeName;

        /// <summary>
        /// Cancellation for all test jobs
        /// </summary>
        private static CancellationTokenSource cancellationSource;

        /// <summary>
        /// The default test parameters
        /// </summary>
        private static Dictionary<string, string> defaultTestParameters;

        /// <summary>
        /// The distributed test client
        /// </summary>
        private static DistributedJobControllerSvc.DistributedJobControllerSvcClient distTestClient;

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
        /// The create test will create a large number of small trees (child number 0 - 20)
        /// and a small number (20 - 50) large trees, which have a lot more children.
        /// Each thread in the test switches between creating small trees and large trees continuously.
        /// So this magic number actually means, after creating one small tree,
        /// how many large tree nodes should it create.
        /// </summary>
        private static int largeTreeRatio = 50;

        /// <summary>
        /// The watcher count per node
        /// </summary>
        private static int watcherCountPerNode = 0;

        /// <summary>
        /// The application settings
        /// </summary>
        private static IConfiguration appSettings;

        /// <summary>
        /// The empty message
        /// </summary>
        private static Empty emptyMessage = new Empty();

        /// <summary>
        /// The GRPC channel
        /// </summary>
        private static Channel grpcChannel;

        /// <summary>
        /// Initializes the test class
        /// </summary>
        /// <param name="context">Test context object</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var path = Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            appSettings = builder.Build();

            Helpers.SetupTraceLog(Path.Combine(appSettings["LogFolder"], "VegaDistributedTestClient.LogPath"));
            log = (s) => Trace.TraceInformation(s);

            cancellationSource = new CancellationTokenSource();
            serverAddress = appSettings["ServerAddress"];
            vegaAddress = appSettings["VegaAddress"];
            vegaPortNumber = appSettings["VegaPortNumber"];
            killerNodeName = appSettings["KillerNodeName"];

            minDataSize = int.Parse(appSettings["MinDataSize"]);
            maxDataSize = int.Parse(appSettings["MaxDataSize"]);
            batchOpCount = int.Parse(appSettings["BatchOpCount"]);
            testCaseSeconds = int.Parse(appSettings["TestCaseSeconds"]);
            requestTimeout = int.Parse(appSettings["RequestTimeout"]);
            threadCount = int.Parse(appSettings["ThreadCount"]);
            asyncTaskCount = int.Parse(appSettings["AsyncTaskCount"]);
            largeTreeRatio = int.Parse(appSettings["LargeTreeRatio"]);
            watcherCountPerNode = int.Parse(appSettings["WatcherCountPerNode"]);

            testProperties = context.Properties;

            // If TAEF provides some parameters, take them and override app.config
            if (testProperties != null)
            {
                if (testProperties.ContainsKey("ServerAddress"))
                {
                    serverAddress = testProperties["ServerAddress"] as string;
                }

                if (testProperties.ContainsKey("VegaAddress"))
                {
                    vegaAddress = testProperties["VegaAddress"] as string;
                }

                if (testProperties.ContainsKey("VegaPortNumber"))
                {
                    vegaPortNumber = testProperties["VegaPortNumber"] as string;
                }

                if (testProperties.ContainsKey("KillerNodeName"))
                {
                    killerNodeName = testProperties["KillerNodeName"] as string;
                }

                if (testProperties.ContainsKey("RootNodeName"))
                {
                    rootNodeName = testProperties["RootNodeName"] as string;
                }
            }
            else
            {
                // Only handle Ctrl-C when running from main()
                Console.CancelKeyPress += (sender, args) =>
                {
                    if (!cancellationSource.IsCancellationRequested)
                    {
                        args.Cancel = true;
                        cancellationSource.Cancel();

                        log($"Ctrl-C intercepted.  Graceful shutdown started.  Press Ctrl-C again to abort.");
                    }
                };
            }

            defaultTestParameters = new Dictionary<string, string>
            {
                { "VegaAddress", vegaAddress },
                { "VegaPort", vegaPortNumber },
                { "RootNodeName", rootNodeName },
                { "MinDataSize", minDataSize.ToString() },
                { "MaxDataSize", maxDataSize.ToString() },
                { "BatchOpCount", batchOpCount.ToString() },
                { "TestCaseSeconds", testCaseSeconds.ToString() },
                { "RequestTimeout", requestTimeout.ToString() },
                { "ThreadCount", threadCount.ToString() },
                { "AsyncTaskCount", asyncTaskCount.ToString() },
                { "LargeTreeRatio", largeTreeRatio.ToString() },
                { "WatcherCountPerNode", watcherCountPerNode.ToString() },
            };

            TestInitialize().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        [ClassCleanup]
        public static void TestClassCleanup()
        {
            cancellationSource.Dispose();
            grpcChannel.ShutdownAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disposes this object
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Pings Madari service using TCP connection and measures the connection time
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestTcpPing()
        {
            this.RunTestJobAsync(
                "TestTcpPing",
                new Dictionary<string, string>
                {
                    { "Hosts", vegaAddress },
                    { "Ports", vegaPortNumber },
                }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ping-pong test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestPingPong()
        {
            this.RunTestJobAsync(
                "TestPingPong",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// create node test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestCreateNode()
        {
            this.RunTestJobAsync(
                "TestCreateNode",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// get full subtree test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestGetFullSubtree()
        {
            this.RunTestJobAsync(
                "TestGetFullSubtree",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// get node test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestGetNode()
        {
            this.RunTestJobAsync(
                "TestGetNode",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// set node test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestSetNode()
        {
            this.RunTestJobAsync(
                "TestSetNode",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// batch create node test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestBatchCreate()
        {
            this.RunTestJobAsync(
                "TestBatchCreate",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// multi create node test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestMultiCreate()
        {
            this.RunTestJobAsync(
                "TestMultiCreate",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Delete node test.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestDeleteNode()
        {
            this.RunTestJobAsync(
                "TestDeleteNode",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Test the NSM/LNM VNET Publishing
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestLnmVnetPublishingScenario()
        {
            this.RunTestJobAsync(
                "TestLnmVnetPublishingScenario",
                defaultTestParameters,
                new string[] { "ProcessedCounts" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Test the Publish/Subscribe scenario.
        /// </summary>
        [TestMethod]
        [TestCategory("Functional")]
        public void TestPublishSubscribeScenario()
        {
            this.RunTestJobAsync(
                "TestPublishSubscribeScenario",
                new Dictionary<string, string>
                    {
                        { "VegaAddress", vegaAddress },
                        { "VegaPort", vegaPortNumber },
                        { "ThreadCount", threadCount.ToString() },
                        { "MinDataSize", minDataSize.ToString() },
                        { "MaxDataSize", maxDataSize.ToString() },
                        { "RequestTimeout", requestTimeout.ToString() },
                        { "TestRepetitions", appSettings["TestRepetitions"] },
                        { "PartitionCount", appSettings["PartitionCount"] },
                        { "NodeCountPerPartition", appSettings["NodeCountPerPartition"] },
                        { "ChannelCount", appSettings["ChannelCount"] },
                    },
                new string[] { "CreateLatency", "ReadLatency", "SetLatency", "DeleteLatency", "InstallWatcherLatency" })
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Async entry of the console program
        /// </summary>
        /// <returns>async task</returns>
        private static async Task TestInitialize()
        {
            string endpoint = $"{serverAddress}:18600";

            if (string.IsNullOrEmpty(serverAddress))
            {
                using (var fabricClient = new FabricClient())
                {
                    endpoint = await GetFirstJobControllerEndpoint(fabricClient).ConfigureAwait(false);
                }
            }

            grpcChannel = new Channel(endpoint, ChannelCredentials.Insecure);
            distTestClient = new DistributedJobControllerSvc.DistributedJobControllerSvcClient(grpcChannel);

            var ids = await distTestClient.GetServiceInstanceIdentitiesAsync(emptyMessage);
            log($"Number of runners: {ids.ServiceInstanceIdentities.Count}");
            log(string.Join("\n", ids.ServiceInstanceIdentities));
        }

        /// <summary>
        /// Get the endpoint of the first job controller micro-service
        /// </summary>
        /// <param name="fabricClient">Service fabric client for querying the naming service</param>
        /// <returns>Endpoint string of the job controller</returns>
        private static async Task<string> GetFirstJobControllerEndpoint(FabricClient fabricClient)
        {
            var nameFilter = new Regex("VegaDistTest", RegexOptions.IgnoreCase);

            var query = fabricClient.QueryManager;
            foreach (var app in await query.GetApplicationListAsync().ConfigureAwait(false))
            {
                if (!nameFilter.IsMatch(app.ApplicationTypeName))
                {
                    continue;
                }

                foreach (var service in await query.GetServiceListAsync(app.ApplicationName).ConfigureAwait(false))
                {
                    if (!nameFilter.IsMatch(service.ServiceTypeName))
                    {
                        continue;
                    }

                    foreach (var partition in await query.GetPartitionListAsync(service.ServiceName).ConfigureAwait(false))
                    {
                        foreach (var replica in await query.GetReplicaListAsync(partition.PartitionInformation.Id).ConfigureAwait(false))
                        {
                            var match = Regex.Match(replica.ReplicaAddress, "\"GrpcEndpoint\":\"([^\"]+)\"");
                            if (match.Success)
                            {
                                return $"{match.Groups[1].Value.Replace(@"\", string.Empty).Replace(@"http://", string.Empty)}";
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Runs a test job
        /// </summary>
        /// <param name="scenarioName">Class name of the test job</param>
        /// <param name="parameters">Test parameters in key-value pairs</param>
        /// <param name="metricNames">metrics that will be pulled from job</param>
        /// <param name="parameterOverrideName">specify the parameter override file name, default to scenarioName</param>
        /// <returns>async task</returns>
        private async Task RunTestJobAsync(
            string scenarioName,
            Dictionary<string, string> parameters,
            string[] metricNames = null,
            string parameterOverrideName = null)
        {
            // try to read overriden parameters from current directory
            string parameterOverrideFile = $"{parameterOverrideName ?? scenarioName}.ParameterOverride";
            if (File.Exists(parameterOverrideFile))
            {
                foreach (var line in File.ReadLines(parameterOverrideFile))
                {
                    bool isOverriden = false;
                    var pair = line.Split(new char[] { ',' });
                    if (pair.Count() == 2)
                    {
                        var key = pair[0].Trim();
                        var value = pair[1].Trim();
                        if (parameters.ContainsKey(key))
                        {
                            parameters[key] = value;
                            isOverriden = true;
                        }
                    }

                    if (!isOverriden)
                    {
                        log($"invalid overriding parameter: {line}.");
                    }
                }
            }

            var startTime = DateTime.Now;
            log($"Starting test {scenarioName} with below parameters");

            foreach (var pair in parameters)
            {
                log($"\t{pair.Key}, {pair.Value}");
            }

            int maxRunningTime = 0;
            if (parameters.ContainsKey("MaxRunningTime"))
            {
                maxRunningTime = int.Parse(parameters["MaxRunningTime"]);
            }

            await distTestClient.StartJobAsync(new StartJobRequest()
            {
                Scenario = scenarioName,
                Parameters = { GrpcHelper.GetJobParametersFromDictionary(parameters) },
            });

            JobState[] jobStates = null;

            while (!cancellationSource.Token.IsCancellationRequested)
            {
                if (maxRunningTime > 0 && (DateTime.Now - startTime).TotalSeconds > maxRunningTime)
                {
                    log($"Hit the max running time ({maxRunningTime} seconds), cancelling jobs...");
                    await distTestClient.CancelRunningJobAsync(emptyMessage);
                    break;
                }

                try
                {
                    jobStates = (await distTestClient.GetJobStatesAsync(emptyMessage)).JobStates.ToArray();
                }
                catch (Exception ex)
                {
                    log($"Exception hit while reading job states from controller, will retry in 10 seconds... ex: {ex}");
                    await Task.Delay(10000, cancellationSource.Token).ConfigureAwait(false);
                    continue;
                }

                log($"{DateTime.Now} - Job states:");

                foreach (var jobState in jobStates)
                {
                    if (jobState != null)
                    {
                        log(jobState.ToString());
                    }
                    else
                    {
                        // should investigate why service returns null.
                        log("NULL state ...");
                    }
                }

                if (jobStates.All(j => j.Completed))
                {
                    if (metricNames != null)
                    {
                        log($"Fetching metrics...");
                        foreach (var metricName in metricNames)
                        {
                            int startIndex = 0;
                            int pageSize = 1000;
                            List<double> metrics = new List<double>();

                            while (!cancellationSource.Token.IsCancellationRequested)
                            {
                                var temp = (await distTestClient.GetJobMetricsAsync(new GetJobMetricsRequest
                                {
                                    MetricName = metricName,
                                    StartIndex = startIndex,
                                    PageSize = pageSize,
                                })).JobMetrics;

                                if (temp.Count == 0)
                                {
                                    break;
                                }

                                startIndex += pageSize;
                                metrics.AddRange(temp);
                            }

                            if (metrics.Count() > 0)
                            {
                                log($"Test outcome {scenarioName} - {metricName} : {Utilities.GetReport(metrics.ToArray())}");
                            }
                        }
                    }

                    break;
                }
                else
                {
                    try
                    {
                        await Task.Delay(10000, cancellationSource.Token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }

            if (cancellationSource.Token.IsCancellationRequested)
            {
                await distTestClient.CancelRunningJobAsync(emptyMessage);
            }

            log($"Test was run with below parameters.");
            foreach (var pair in parameters)
            {
                log($"\t{pair.Key}, {pair.Value}");
            }

            log($"Test duration: {(DateTime.Now - startTime).TotalSeconds} seconds. Range Local: {startTime} - {DateTime.Now}, UTC: {startTime.ToUniversalTime()} - {DateTime.Now.ToUniversalTime()}");

            Assert.AreEqual(0, jobStates.Count(jobState => !jobState.Passed));
        }
    }
}
