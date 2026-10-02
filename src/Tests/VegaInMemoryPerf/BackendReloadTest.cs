// <copyright file="BackendReloadTest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RequestResponse = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestResponse;

    /// <summary>
    /// Test the backend reload.
    /// </summary>
    [TestClass]
    public class BackendReloadTest
    {
        private int unobservedExceptionCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendReloadTest"/> class.
        /// </summary>
        public BackendReloadTest()
        {
            var hasConsoleListener = false;
            foreach (var lis in Trace.Listeners)
            {
                if (lis is ConsoleTraceListener)
                {
                    hasConsoleListener = true;
                }
            }

            if (!hasConsoleListener)
            {
                Trace.Listeners.Add(new ConsoleTraceListener());
            }

            TaskScheduler.UnobservedTaskException -= UnobservedExceptionHandler;
            TaskScheduler.UnobservedTaskException += UnobservedExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            void UnobservedExceptionHandler(object sender, UnobservedTaskExceptionEventArgs args)
            {
                Trace.TraceError($"Unobserved exception: {args.Exception}");
                Interlocked.Increment(ref this.unobservedExceptionCount);
                args.SetObserved();
            }

            void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
            {
                var ex = args.ExceptionObject as Exception;
                Trace.TraceError($"Unhandled exception: {ex.Message}");
                Interlocked.Increment(ref this.unobservedExceptionCount);
            }
        }

        /// <summary>
        /// Tests when backend restart no null reference exception.
        /// </summary>
        [TestMethod]
        public void TestBackendRestartNoNullRefException()
        {
            var backendStarted = new ManualResetEventSlim();
            var persistFactory = new MultiInstancePersistedDataFactory("partition", "persistFactory", CancellationToken.None);
            var backendCore = new RingMasterBackendCore(persistFactory);
            backendCore.OnBackendRestartFailure = o =>
            {
                throw new Exception("Backend failed to restart");
            };

            backendCore.Start(CancellationToken.None);
            backendCore.OnBecomePrimary();
            backendCore.StartService = (p1, p2) => { backendStarted.Set(); };

            Assert.IsTrue(backendStarted.Wait(30000));

            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();
            var backendServer = new RingMasterServer(protocol, null, CancellationToken.None);

            var transportConfig = new SecureTransport.Configuration
            {
                UseSecureConnection = false,
                IsClientCertificateRequired = false,
                CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
            };

            var serverTransport = new SecureTransport(transportConfig);

            backendServer.RegisterTransport(serverTransport);
            backendServer.OnInitSession = initRequest =>
            {
                return new CoreRequestHandler(backendCore, initRequest);
            };

            serverTransport.StartServer(10001);
            var serverAddress = "127.0.0.1:10001";

            var stop = false;
            Task.Run(async () =>
            {
                using (var client = new RingMasterClient(serverAddress, null, null, 2000))
                {
                    while (!stop)
                    {
                        try
                        {
                            var stat = await client.Exists($"/", null);
                            Trace.TraceInformation($"Exists returned. {stat.Ctime}");
                        }
                        catch (RingMasterException)
                        {
                        }

                        await Task.Delay(1000);
                    }
                }
            });

            int restartCount = 0;
            var clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < 2 * 60 * 1000)
            {
                restartCount++;
                backendStarted.Reset();
                Trace.TraceInformation($"Restarting backendcore: round {restartCount}");
                backendCore.OnPrimaryStatusLost();

                backendCore.Start(CancellationToken.None);
                backendCore.OnBecomePrimary();
                Assert.IsTrue(backendStarted.Wait(30000));

                Thread.Sleep(1000);
            }

            stop = true;
        }

        /// <summary>
        /// Tests when backend restart it won't stuck.
        /// </summary>
        [TestMethod]
        public void TestBackendRestartShouldNotStuck()
        {
            int restartCount = 0;
            var clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < 5 * 60 * 1000)
            {
                Thread.Sleep(2000);

                var persistFactory = new MultiInstancePersistedDataFactory("partition", "persistFactory", CancellationToken.None);
                var backendCore = new RingMasterBackendCore(persistFactory);
                backendCore.OnBackendRestartFailure = o =>
                {
                    // throw new Exception("Backend failed to restart");
                    Trace.TraceError("Backend failed to restart");
                };

                backendCore.Start(CancellationToken.None);
                backendCore.OnBecomePrimary();

                Trace.TraceInformation($"Restarting backendcore: round {restartCount++}");
                backendCore.OnPrimaryStatusLost();

                Thread.Sleep(1000);

                var backendStarted = new ManualResetEventSlim();

                // restarting a new backendcore instance.
                backendCore.Start(CancellationToken.None);
                backendCore.OnBecomePrimary();
                backendCore.StartService = (p1, p2) => { backendStarted.Set(); };

                Assert.IsTrue(backendStarted.Wait(20000), "backend couldnt start on time");
            }
        }

        /// <summary>
        /// Single partition stateful service.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void SinglePartitionReload()
        {
            Assert.AreEqual(0, this.TestMultiplePartitionReload(1, 0.0));
            Assert.AreEqual(0, this.unobservedExceptionCount);
        }

        /// <summary>
        /// Three partitions stateful service.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void ThreePartitionsReload()
        {
            Assert.AreEqual(0, this.TestMultiplePartitionReload(3, 0.0));
            Assert.AreEqual(0, this.unobservedExceptionCount);
        }

        /// <summary>
        /// Three partitions stateful service with replica removal
        /// </summary>
        [TestMethod]
        [Ignore]
        public void ThreePartitionsReloadWithReplicaRemoval()
        {
            Assert.AreEqual(0, this.TestMultiplePartitionReload(3, 0.20));
            Assert.AreEqual(0, this.unobservedExceptionCount);
        }

        private int TestMultiplePartitionReload(
            int numberOfPartitions,
            double primaryRemovalProbability = 0.05,
            int testDurationInMs = 1000_000,
            int primaryChangeInternalInMs = 1000)
        {
            int errorCount = 0;

            Parallel.For(
                0,
                numberOfPartitions,
                partitionIndex =>
                {
                    var partitionName = $"MockService-{partitionIndex}";
                    var services = Enumerable.Range(0, 3).Select(_ => new MockStatefulService(partitionName)).ToList();

                    try
                    {
                        var req = this.GenerateRequests().GetEnumerator();
                        var rnd = new Random();
                        var clock = Stopwatch.StartNew();
                        while (clock.ElapsedMilliseconds < testDurationInMs)
                        {
                            var primaryIndex = rnd.Next(services.Count - 1);
                            var startTime = clock.ElapsedMilliseconds;
                            using (var cancellationSource = new CancellationTokenSource())
                            {
                                services[primaryIndex].RunAsync(cancellationSource.Token);

                                services[primaryIndex].PrimaryInitialized.Wait();

                                // Send some requests
                                while (clock.ElapsedMilliseconds - startTime < primaryChangeInternalInMs)
                                {
                                    req.MoveNext();
                                    var (request, verify) = req.Current;
                                    var (resp, ex) = services[primaryIndex].Request(request);
                                    if (ex != null)
                                    {
                                        Interlocked.Increment(ref this.unobservedExceptionCount);
                                        Trace.TraceError($"Request failed in {partitionName}: {ex}");
                                    }
                                    else
                                    {
                                        Assert.IsTrue(verify(resp), $"Response verification failed: {resp.ToString()}");
                                    }
                                }

                                cancellationSource.Cancel();
                            }

                            if (rnd.NextDouble() <= primaryRemovalProbability)
                            {
                                Trace.TraceInformation($"Removing replica {primaryIndex} in {partitionName} and adding a new one");
                                services.RemoveAt(primaryIndex);
                                services.Add(new MockStatefulService(partitionName));
                            }
                        }

                        Trace.TraceInformation($"{partitionName} is shutting down after {clock.Elapsed}.");
                    }
                    finally
                    {
                        foreach (var svc in services)
                        {
                            svc.Dispose();
                        }
                    }

                    foreach (var svc in services)
                    {
                        Interlocked.Add(ref errorCount, svc.ErrorCount);
                    }
                });

            return errorCount;
        }

        private IEnumerable<(IRingMasterBackendRequest Request, Func<RequestResponse, bool> Func)> GenerateRequests()
        {
            const int nodeCount = 1024 * 1024;
            while (true)
            {
                // Firstly create some nodes.
                for (int i = 0; i < nodeCount; i++)
                {
                    yield return (
                        new RequestCreate($"/test/N-{i}", null, null, null, CreateMode.PersistentAllowPathCreation, null, 0),
                        r => r.ResultCode == (int)RingMasterException.Code.Ok && r.Stat.Version == 1);
                }

                // Verify the number of children.
                yield return (
                    new RequestExists("/test", null, null, null),
                    r => r.ResultCode == (int)RingMasterException.Code.Ok && r.Stat.NumChildren == nodeCount);

                // Verify the reading of data.
                for (int i = 0; i < nodeCount; i++)
                {
                    yield return (
                        new RequestGetData($"/test/N-{i}", null, null, null),
                        r => r.ResultCode == (int)RingMasterException.Code.Ok);
                }

                // Updates those nodes.
                for (int k = 0; k < 10; k++)
                {
                    for (int i = 0; i < nodeCount; i++)
                    {
                        yield return (
                            new RequestSetData($"/test/N-{i}", null, null, -1, null),
                            r => r.ResultCode == (int)RingMasterException.Code.Ok && r.Stat.Version == k + 2);
                    }
                }

                // Delete children one by one.
                for (int i = 0; i < nodeCount; i++)
                {
                    yield return (
                        new RequestDelete($"/test/N-{i}", null, -1, null),
                        r => r.ResultCode == (int)RingMasterException.Code.Ok);
                }

                // Delete the parent node.
                yield return (
                    new RequestDelete("/test", null, -1, null),
                    r => r.ResultCode == (int)RingMasterException.Code.Ok);
            }
        }

        private sealed class ConsoleTraceListener : TraceListener
        {
            public override void Write(string message) => Console.Write(message);

            public override void WriteLine(string message) => Console.WriteLine(message);
        }
    }
}
