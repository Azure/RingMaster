// <copyright file="VegaUpgradeTest.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Extensions.Configuration;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The Vega Service Fabric Performance test class
    /// </summary>
    [TestClass]
    public class VegaUpgradeTest
    {
        private const int CreateBatchCount = 256;
        private const int NodeDataLength = 1024;

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
        private static int requestTimeout = 10000;

        /// <summary>
        /// Total number of data items being processed
        /// </summary>
        private static long totalDataCount = 0;

        /// <summary>
        /// Total number of failures in each test
        /// </summary>
        private static long totalFailures = 0;

        /// <summary>
        /// PowerShell process object for purpose of cleanup
        /// </summary>
        private static Process powershellProcess = null;

        /// <summary>
        /// Precise stopwatch for measuring how long the primary was down
        /// </summary>
        private static Stopwatch clock = Stopwatch.StartNew();

        /// <summary>
        /// Timestamp of the last succeeded operation
        /// </summary>
        private static TimeSpan lastOpTimestamp = TimeSpan.MaxValue;

        /// <summary>
        /// Primary can be down for this amount of time
        /// </summary>
        private static int maxDownTimeInSecond = 300;

        /// <summary>
        /// Class level setup
        /// </summary>
        /// <param name="context">Test context</param>
        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            IConfiguration appSettings = builder.Build();

            Helpers.Helpers.SetupTraceLog(Path.Combine(appSettings["LogFolder"], "VegaUpgradeTest.LogPath"));
            log = s => Trace.TraceInformation($"{DateTime.Now} {s}");

            if (context.Properties.ContainsKey("ServerAddress"))
            {
                serverAddress = context.Properties["ServerAddress"] as string;
            }

            if (context.Properties.ContainsKey("MaxDownTimeInSecond"))
            {
                maxDownTimeInSecond = int.Parse(context.Properties["MaxDownTimeInSecond"] as string);
            }
        }

        /// <summary>
        /// Cleanup at class level to kill PowerShell
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (powershellProcess != null && !powershellProcess.HasExited)
            {
                powershellProcess.Kill();
                powershellProcess.Dispose();
            }
        }

        /// <summary>
        /// Create data node performance
        /// </summary>
        [TestMethod]
        public void TestUpgrade()
        {
            var downTimeList = new List<double>();

            try
            {
                TestUpgradeAsync().GetAwaiter().GetResult();
            }
            finally
            {
                log($"Downtime: " + string.Join(", ", downTimeList));
            }

            async Task TestUpgradeAsync()
            {
                using (var cancellation = new CancellationTokenSource())
                {
                    var createNodeTask = Task.Run(() => CreateNodeThread(cancellation.Token));

                    var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var upgradeTask = Task.Run(() => StartUpgradeScript(
                        Path.Combine(currentDirectory, @"..\CloudUnittests\RunSelfUpgrade.ps1"),
                        Path.Combine(currentDirectory, @"upgradetest-out.log"),
                        Path.Combine(currentDirectory, @"upgradetest-err.log")));

                    var downTime = 0.0;
                    var lastCount = -1L;
                    while (!upgradeTask.IsCompleted)
                    {
                        await Task.Delay(1000);

                        var delta = lastCount > 0 ? totalDataCount - lastCount : 0;
                        lastCount = totalDataCount;

                        log($"Count={totalDataCount} +{delta} Failed={totalFailures}");

                        var currentDownTime = (clock.Elapsed - lastOpTimestamp).TotalSeconds;
                        if (currentDownTime > downTime)
                        {
                            Assert.IsTrue(
                                currentDownTime < maxDownTimeInSecond,
                                $"Service endpoint {serverAddress} is down over {maxDownTimeInSecond} seconds");
                        }
                        else if (downTime > 2.0)
                        {
                            downTimeList.Add(downTime);
                        }

                        downTime = currentDownTime;
                    }
                }
            }
        }

        private static void StartUpgradeScript(string scriptPath, string stdoutFile, string stderrFile)
        {
            var processStartInfo = new ProcessStartInfo("powershell.exe")
            {
                Arguments = $"-ExecutionPolicy bypass {scriptPath} -Verbose",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (powershellProcess = Process.Start(processStartInfo))
            {
                powershellProcess.OutputDataReceived += (_, outline) =>
                {
                    if (!string.IsNullOrEmpty(outline.Data))
                    {
                        File.AppendAllLines(stdoutFile, new[] { outline.Data });
                    }
                };

                powershellProcess.ErrorDataReceived += (_, errline) =>
                {
                    if (!string.IsNullOrEmpty(errline.Data))
                    {
                        File.AppendAllLines(stderrFile, new[] { errline.Data });
                    }
                };

                powershellProcess.BeginOutputReadLine();
                powershellProcess.BeginErrorReadLine();
                powershellProcess.WaitForExit();
            }

            powershellProcess = null;
        }

        /// <summary>
        /// Work load for testing Create method
        /// </summary>
        private static void CreateNodeThread(CancellationToken cancellationToken)
        {
            const string rootNodeName = "UpgradeTest";
            RingMasterClient client = null;

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

                Parallel.For(
                    0,
                    CreateBatchCount,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2, },
                    (i, loopState) =>
                    {
                        var path = $"/{rootNodeName}/{totalDataCount}";
                        var data = Enumerable.Range(0, NodeDataLength).Select(x => (byte)(x + i)).ToArray();

                        try
                        {
                            client.Create(
                                path,
                                data,
                                null,
                                CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag)
                                .GetAwaiter().GetResult();
                            client.GetData(path, false).GetAwaiter().GetResult();

                            Interlocked.Increment(ref totalDataCount);
                            lastOpTimestamp = clock.Elapsed;
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref totalFailures);
                            client = null;

                            log($"Exception: {ex.Message}");

                            loopState.Stop();
                        }
                    });
            }
        }
    }
}
