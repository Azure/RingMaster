// <copyright file="VegaInMemoryPerf.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Networking.Infrastructure.RingMaster;
    using Azure.Networking.Infrastructure.RingMaster.Backend;
    using Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Azure.Networking.Infrastructure.RingMaster.Communication;
    using Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Azure.Networking.Infrastructure.RingMaster.Data;
    using Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using Azure.Networking.Infrastructure.RingMaster.Server;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Backend Core Performance Test using in memory persistence
    /// </summary>
    [TestClass]
    public sealed class VegaInMemoryPerf
    {
        /// <summary>
        /// Logging support
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Backend core
        /// </summary>
        private static RingMasterBackendCore backendCore;

        /// <summary>
        /// <see cref="RingMasterCommunicationProtocol"/> is used as the communication protocol.
        /// </summary>
        private static ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

        /// <summary>
        /// Persisted data factory.
        /// </summary>
        private static InMemoryFactory inMemoryFactory = new InMemoryFactory(true, null, CancellationToken.None);

        /// <summary>
        /// Backend server
        /// </summary>
        private static RingMasterServer backendServer;

        /// <summary>
        /// Server transport
        /// </summary>
        private static SecureTransport serverTransport;

        /// <summary>
        /// Minimum size of node payload
        /// </summary>
        private static int minPayloadSize;

        /// <summary>
        /// Maximum size of node payload
        /// </summary>
        private static int maxPayloadSize;

        /// <summary>
        /// Number of thread to exercise the backend
        /// </summary>
        private static int threadCount;

        /// <summary>
        /// Start the backend server
        /// </summary>
        /// <param name="context">Test context</param>
        [AssemblyInitialize]
        public static void Setup(TestContext context)
        {
            log = s => context.WriteLine(s);

            backendCore = CreateBackend();
            backendServer = new RingMasterServer(protocol, null, CancellationToken.None);

            var transportConfig = new SecureTransport.Configuration
            {
                UseSecureConnection = false,
                IsClientCertificateRequired = false,
                CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion
            };

            serverTransport = new SecureTransport(transportConfig);

            backendServer.RegisterTransport(serverTransport);
            backendServer.OnInitSession = initRequest =>
            {
                return new CoreRequestHandler(backendCore, initRequest);
            };

            serverTransport.StartServer(10009);

            // Read the app settings
            minPayloadSize = int.Parse(ConfigurationManager.AppSettings["MinPayloadSize"]);
            maxPayloadSize = int.Parse(ConfigurationManager.AppSettings["MaxPayloadSize"]);
            threadCount = int.Parse(ConfigurationManager.AppSettings["ThreadCount"]);
        }

        /// <summary>
        /// Cleanup the backend server
        /// </summary>
        [AssemblyCleanup]
        public static void Cleanup()
        {
            using (var cancellationSource = new CancellationTokenSource())
            {
                var cancel = cancellationSource.Token;

                Task.Run(() =>
                {
                    try
                    {
                        Task.Delay(10 * 1000, cancel);
                        Assert.Fail("Server cannot be stopped");
                        Environment.Exit(-1);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                });

                serverTransport.Stop();
                cancellationSource.Cancel();
            }
        }

        /// <summary>
        /// Test the Create Node scenario
        /// </summary>
        [TestMethod]
        public void TestCreateSingleThread()
        {
            using (var cancellationSource = new CancellationTokenSource())
            {
                var cancel = cancellationSource.Token;

                using (var client = new RingMasterClient("127.0.0.1:10009", null, null, 10000))
                {
                    var rnd = new Random();
                    var createCount = 0;
                    var dataSize = 0;

                    Task.Run(async () =>
                    {
                        while (!cancel.IsCancellationRequested)
                        {
                            var data = new byte[rnd.Next(minPayloadSize, maxPayloadSize)];
                            var path = await client.Create($"/Perf/{createCount}", data, null, CreateMode.PersistentAllowPathCreation);
                            Assert.AreNotEqual(null, path);

                            Interlocked.Increment(ref createCount);
                            Interlocked.Add(ref dataSize, data.Length);
                        }
                    });

                    Task.Run(async () =>
                    {
                        var lastCount = createCount;

                        while (!cancel.IsCancellationRequested)
                        {
                            await Task.Delay(1000);
                            var delta = createCount - lastCount;
                            lastCount = createCount;

                            log($"{DateTime.Now} createCount={createCount} +{delta} dataSize={dataSize}");
                        }
                    });

                    Thread.Sleep(10 * 1000);
                    cancellationSource.Cancel();
                    log($"CreateCount = {createCount}");
                }
            }
        }

        /// <summary>
        /// Test the NSM/LNM VNET Publishing
        /// </summary>
        [TestMethod]
        public void TestLnmVnetPublishingScenario()
        {
            using (var cancellationSource = new CancellationTokenSource())
            {
                var operationCount = new OperationCount();
                var cancel = cancellationSource.Token;
                var threads = Enumerable.Range(0, threadCount)
                    .Select(n => new Thread(() => this.MockLnmThread(cancel, n, operationCount)))
                    .ToArray();
                Parallel.ForEach(threads, t => t.Start());

                Task.Run(async () =>
                {
                    var lastCount = operationCount.CreateCount;

                    while (!cancel.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                        var delta = operationCount.CreateCount - lastCount;
                        lastCount = operationCount.CreateCount;

                        log($"{DateTime.Now} createCount={operationCount.CreateCount} +{delta} dataSize={operationCount.DataSize}");
                    }
                });

                Thread.Sleep(100 * 1000);
                cancellationSource.Cancel();
                log($"CreateCount = {operationCount.CreateCount} SetCount = {operationCount.SetCount} Failures = {operationCount.FailureCount}");
            }
        }

        /// <summary>
        /// Creates a VNET ID spanning across multiple cluster, which is mimicked by thread
        /// </summary>
        /// <param name="threadId">Thread sequence number</param>
        /// <returns>A random VNET ID in string</returns>
        private static string CreateSpanningVnetId(int threadId)
        {
            return string.Concat(DateTime.UtcNow.ToString("HHmmss"), threadId);
        }

        /// <summary>
        /// Creates a new backend with an in-memory store
        /// </summary>
        /// <returns>Backend instance</returns>
        private static RingMasterBackendCore CreateBackend()
        {
            RingMasterBackendCore backend = null;
            try
            {
                var backendStarted = new ManualResetEventSlim();

                backend = new RingMasterBackendCore(inMemoryFactory);

                backend.StartService = (p1, p2) => { backendStarted.Set(); };
                backend.Start();
                backend.OnBecomePrimary();

                Assert.IsTrue(backendStarted.Wait(30000));
                var backendToReturn = backend;
                backend = null;
                return backendToReturn;
            }
            finally
            {
                if (backend != null)
                {
                    backend.Dispose();
                }
            }
        }

        /// <summary>
        /// Thread to mock a NSM / LNM which is publishing VNET data
        /// </summary>
        /// <param name="cancel">Cancellation token</param>
        /// <param name="threadId">Thread sequence number</param>
        /// <param name="operationCount">Object to store operation statistics</param>
        private void MockLnmThread(CancellationToken cancel, int threadId, OperationCount operationCount)
        {
            using (var client = new RingMasterClient("127.0.0.1:10009", null, null, 10000))
            {
                var rnd = new Random();

                while (!cancel.IsCancellationRequested)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var vnet = $"/mud/vnets/{CreateSpanningVnetId(threadId)}";
                            var stat = await client.Exists(vnet, null, true);
                            var ops = new List<Op>();

                            if (stat == null)
                            {
                                ops.Add(Op.Create($"{vnet}/mappings/v4ca", null, null, CreateMode.PersistentAllowPathCreation));
                                ops.Add(Op.Create($"{vnet}/lnms/thread-{threadId}", null, null, CreateMode.PersistentAllowPathCreation));

                                await client.Multi(ops, true);
                                ops.Clear();

                                operationCount.AddCreate(2);
                            }

                            var mappingCount = rnd.Next(1, 1024 * 8);
                            for (int i = 0; i < mappingCount; i++)
                            {
                                ops.Add(Op.Create($"{vnet}/mappings/v4ca/{i}", null, null, CreateMode.PersistentAllowPathCreation));
                                operationCount.AddCreate(1);
                            }

                            await client.Multi(ops, true);
                            ops.Clear();

                            for (int i = 0; i < mappingCount; i++)
                            {
                                var data = new byte[rnd.Next(minPayloadSize, maxPayloadSize)];
                                ops.Add(Op.SetData($"{vnet}/mappings/v4ca/{i}", data, -1));
                                operationCount.AddSet(1);
                                operationCount.AddData(data.Length);
                            }

                            await client.Multi(ops, true);
                            ops.Clear();
                        }
                        catch (Exception ex)
                        {
                            operationCount.IncrementFailure();

                            // Ignore and keep going
                            log($"FAIL in {threadId}: {ex}");
                        }
                    }).GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Store the operation statistics, count of operations, etc.
        /// </summary>
        private sealed class OperationCount
        {
            /// <summary>
            /// Count of create operation
            /// </summary>
            private long createCount = 0;

            /// <summary>
            /// Count of set operation
            /// </summary>
            private long setCount = 0;

            /// <summary>
            /// Total data size
            /// </summary>
            private long dataSize = 0;

            /// <summary>
            /// Total number of failures
            /// </summary>
            private int failureCount = 0;

            /// <summary>
            /// Gets the create count
            /// </summary>
            public long CreateCount => this.createCount;

            /// <summary>
            /// Gets the set count
            /// </summary>
            public long SetCount => this.setCount;

            /// <summary>
            /// Gets the total data size
            /// </summary>
            public long DataSize => this.dataSize;

            /// <summary>
            /// Gets the total failure count
            /// </summary>
            public int FailureCount => this.failureCount;

            /// <summary>
            /// Adds the specified count to create count
            /// </summary>
            /// <param name="count">Count to be added</param>
            public void AddCreate(int count)
            {
                Interlocked.Add(ref this.createCount, count);
            }

            /// <summary>
            /// Adds the specified count to set count
            /// </summary>
            /// <param name="count">Count to be added</param>
            public void AddSet(int count)
            {
                Interlocked.Add(ref this.setCount, count);
            }

            /// <summary>
            /// Add the specified size to the total data size
            /// </summary>
            /// <param name="size">Size to be added</param>
            public void AddData(int size)
            {
                Interlocked.Add(ref this.dataSize, size);
            }

            /// <summary>
            /// Increments the count of failures
            /// </summary>
            public void IncrementFailure()
            {
                Interlocked.Increment(ref this.failureCount);
            }
        }
    }
}
