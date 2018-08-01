// <copyright file="VegaInMemoryPerf.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
    using Azure.Networking.Infrastructure.RingMaster.Transport;
    using Extensions.Configuration;
    using Microsoft.Vega.Test.Helpers;
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
        /// Endpoint address of the backend server
        /// </summary>
        private static string serverAddress;

        /// <summary>
        /// Start the backend server
        /// </summary>
        /// <param name="context">Test context</param>
        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            IConfiguration appSettings = builder.Build();

            Helpers.SetupTraceLog(Path.Combine(appSettings["LogFolder"], "VegaInMemoryPerf.LogPath"));
            log = s => Trace.TraceInformation(s);

            // If a parameter is specified as follows:
            //      te.exe VegaInMemoryPerf.dll /p:ServerAddress=127.0.0.1:99
            if (!context.Properties.ContainsKey("ServerAddress"))
            {
                backendCore = CreateBackend();
                backendServer = new RingMasterServer(protocol, null, CancellationToken.None);

                var transportConfig = new SecureTransport.Configuration
                {
                    UseSecureConnection = false,
                    IsClientCertificateRequired = false,
                    CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                };

                serverTransport = new SecureTransport(transportConfig);

                backendServer.RegisterTransport(serverTransport);
                backendServer.OnInitSession = initRequest =>
                {
                    return new CoreRequestHandler(backendCore, initRequest);
                };

                serverTransport.StartServer(10009);
                serverAddress = "127.0.0.1:10009";
            }
            else
            {
                serverAddress = context.Properties["ServerAddress"] as string;
            }

            // Read the app settings
            minPayloadSize = int.Parse(appSettings["MinPayloadSize"]);
            maxPayloadSize = int.Parse(appSettings["MaxPayloadSize"]);
            threadCount = int.Parse(appSettings["ThreadCount"]);
        }

        /// <summary>
        /// Cleanup the backend server
        /// </summary>
        [ClassCleanup]
        public static void Cleanup()
        {
            if (serverTransport == null)
            {
                return;
            }

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

                using (var client = new RingMasterClient(serverAddress, null, null, 10000))
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
                Parallel.ForEach(threads, t => t.Join());
                log($"CreateCount = {operationCount.CreateCount} SetCount = {operationCount.SetCount} Failures = {operationCount.FailureCount}");
            }
        }

        /// <summary>
        /// Test the scenario when one thread tries to get full subtree
        /// while another thread tries to update the subtree,
        /// the first thread should not get inconsistant subtree.
        /// </summary>
        [TestMethod]
        public void TestGetFullSubtreeWhileUpdating()
        {
            TestAsync().GetAwaiter().GetResult();

            async Task TestAsync()
            {
                const int InitialNodeData = 1;
                const int NewNodeData = 2;
                const int ChildrenCount = 50000;
                const string RootName = nameof(this.TestGetFullSubtreeWhileUpdating);

                using (var client = new RingMasterClient(serverAddress, null, null, 100000))
                {
                    byte[] data = BitConverter.GetBytes(InitialNodeData);
                    await client.Create($"/{RootName}/node1", data, null, CreateMode.PersistentAllowPathCreation).ConfigureAwait(false);
                    await client.Create($"/{RootName}/node2", data, null, CreateMode.PersistentAllowPathCreation).ConfigureAwait(false);
                    await client.Create($"/{RootName}/node3", data, null, CreateMode.PersistentAllowPathCreation).ConfigureAwait(false);

                    var ops = new List<Op>(ChildrenCount);
                    for (int count = 0; count < ChildrenCount; count++)
                    {
                        ops.Add(Op.Create($"/{RootName}/node2/{count}", data, null, CreateMode.PersistentAllowPathCreation));
                    }

                    await client.Batch(ops).ConfigureAwait(false);
                }

                ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                Task<TreeNode> getSubtreeTask = new Task<TreeNode>(() =>
                {
                    using (var client = new RingMasterClient(serverAddress, null, null, 10000))
                    {
                        return client.GetFullSubtree($"/{RootName}").Result;
                    }
                });

                Task updateDataTask = Task.Run(async () =>
                {
                    using (var client = new RingMasterClient(serverAddress, null, null, 10000))
                    {
                        var ops = new List<Op>(2);
                        byte[] newData = BitConverter.GetBytes(NewNodeData);

                        ops.Add(Op.SetData($"/{RootName}/node1", newData, -1));
                        ops.Add(Op.SetData($"/{RootName}/node3", newData, -1));

                        manualResetEvent.WaitOne();

                        // this is to make sure the set data occurs after get full substree started.
                        Thread.Sleep(20);
                        await client.Batch(ops).ConfigureAwait(false);
                    }
                });

                getSubtreeTask.Start();
                manualResetEvent.Set();

                await Task.WhenAll(getSubtreeTask, updateDataTask);
                var tree = getSubtreeTask.Result;
                int node1Data = BitConverter.ToInt32(tree.Children[0].Data, 0);
                int node3Data = BitConverter.ToInt32(tree.Children[2].Data, 0);

                Assert.IsTrue(node1Data >= node3Data);
            }
        }

        /// <summary>
        /// Tests the scenario that multiple threads
        /// try to delete the same node at the same time.
        /// Should not throw any exception.
        /// </summary>
        [TestMethod]
        public void TestConcurrentDelete()
        {
            TestAsync().GetAwaiter().GetResult();

            async Task TestAsync()
            {
                const int ChildrenCount = 1000;
                const string RootName = nameof(this.TestConcurrentDelete);
                const int threadCount = 64;

                using (var client = new RingMasterClient(serverAddress, null, null, 10000))
                {
                    await client.Create($"/{RootName}", null, null, CreateMode.PersistentAllowPathCreation).ConfigureAwait(false);

                    var ops = new List<Op>(ChildrenCount);
                    for (int count = 0; count < ChildrenCount; count++)
                    {
                        ops.Add(Op.Create($"/{RootName}/{count}", null, null, CreateMode.PersistentAllowPathCreation));
                    }

                    await client.Batch(ops).ConfigureAwait(false);
                }

                for (int i = 0; i < threadCount; i++)
                {
                    var deleteChildTask = this.DeleteChild(RootName, ChildrenCount);
                    var deleteParent = Task.Run(async () =>
                    {
                        using (var client = new RingMasterClient(serverAddress, null, null, 10000))
                        {
                            await client.Delete($"/{RootName}", -1, DeleteMode.None);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Checks if conflicting write and read requests on the same node cause any race condition. Exists and
        /// SortedArrayList used to be broken with sporadic exceptions.
        /// </summary>
        [TestMethod]
        public void TestConflictingCreateDeleteExists()
        {
            TestConflictingCreateDeleteExistsAsync().GetAwaiter().GetResult();

            async Task TestConflictingCreateDeleteExistsAsync()
            {
                const string path = "/$rmbvt/test";
                var stop = false;
                var taskW = Task.Run(async () =>
                {
                    while (!stop)
                    {
                        try
                        {
                            using (var rm = new RingMasterClient(serverAddress, null, null, 10000))
                            {
                                while (!stop)
                                {
                                    await rm.Create(path, null, null, CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag);
                                    await rm.Delete(path, -1, DeleteMode.SuccessEvenIfNodeDoesntExist);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var rmException = ex as RingMasterException;
                            if (rmException != null && rmException.ErrorCode == RingMasterException.Code.Operationtimeout)
                            {
                                // Do thing. Continue test.
                            }
                            else
                            {
                                stop = true;
                                throw;
                            }
                        }
                    }
                });

                var taskR = Task.Run(async () =>
                {
                    while (!stop)
                    {
                        try
                        {
                            using (var rm = new RingMasterClient(serverAddress, null, null, 10000))
                            {
                                while (!stop)
                                {
                                    await Task.WhenAll(
                                        rm.Exists(path, null, true));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var rmException = ex as RingMasterException;
                            if (rmException != null && rmException.ErrorCode == RingMasterException.Code.Operationtimeout)
                            {
                                // Do thing. Continue test.
                            }
                            else
                            {
                                stop = true;
                                throw;
                            }
                        }
                    }
                });

                var clock = Stopwatch.StartNew();
                SpinWait.SpinUntil(() => clock.Elapsed.TotalSeconds >= 3600 || stop);

                stop = true;
                await Task.WhenAll(taskW, taskR).ContinueWith(t => log(t.Exception?.ToString()));
            }
        }

        /// <summary>
        /// This test verifies that the number of children remains the same after failed Multi.
        /// </summary>
        [TestMethod]
        public void TestWrongChildrenCountAfterFailedMulti()
        {
            TestWrongNumChildrenInStastAsync().GetAwaiter().GetResult();

            async Task TestWrongNumChildrenInStastAsync()
            {
                const string path = "/$rmbvt/test";
                var stop = false;

                // Create a parent node with 3 children. During the test, the number of children is not expected
                // to change.
                using (var rm = new RingMasterClient(serverAddress, null, null, 10000))
                {
                    var ops = new List<Op>
                    {
                        Op.Create($"{path}/parent/child1", null, null, CreateMode.PersistentAllowPathCreation),
                        Op.Create($"{path}/parent/child2", null, null, CreateMode.PersistentAllowPathCreation),
                        Op.Create($"{path}/parent/child3", null, null, CreateMode.PersistentAllowPathCreation),
                    };

                    await rm.Multi(ops);
                }

                // Start multiple threads to stress the backend
                var tasks = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
                {
                    using (var rm = new RingMasterClient(serverAddress, null, null, 10000))
                    {
                        var ops = new List<Op>();

                        while (!stop)
                        {
                            // Randomly add or delete children in Multi
                            ops.Clear();
                            ops.AddRange(
                                Enumerable.Range(1, 3).Select(
                                    x => Op.Delete($"{path}/parent/child{x}", -1, false)));

                            // Add one more operation to fail the multi, so nothing get committed, in other words the
                            // locklist will always abort.
                            ops.Add(Op.GetData(
                                    $"{path}/parent/nonexisting/node",
                                    Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions.None,
                                    null));
                            var result = (await rm.Multi(ops)).Last();
                            Assert.AreEqual(OpCode.Error, result.ResultType);
                            Assert.AreEqual(RingMasterException.Code.Nonode, result.ErrCode);

                            var children = await rm.GetChildren($"{path}/parent", null);
                            var stat = await rm.Exists($"{path}/parent", null);

                            Assert.AreEqual(
                                children.Count,
                                stat.NumChildren,
                                $"Children count {children.Count} should be consistent with Stat {stat.NumChildren}");
                            Assert.AreEqual(
                                3,
                                stat.NumChildren,
                                "Number of children returned by Exists should not change");
                        }
                    }
                })).ToArray();

                var clock = Stopwatch.StartNew();
                while (clock.Elapsed.TotalMinutes < 60)
                {
                    await Task.Delay(1000);
                    if (tasks.Any(t => t.IsCompleted))
                    {
                        break;
                    }
                }

                stop = true;
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// This test verifies that the requests in multi will return proper response
        /// </summary>
        [TestMethod]
        public void TestRequestInMultiReturnCorrectly()
        {
            TestRequestInMultiReturnCorrectlyAsync().GetAwaiter().GetResult();

            async Task TestRequestInMultiReturnCorrectlyAsync()
            {
                const string RootName = nameof(this.TestRequestInMultiReturnCorrectly);
                using (var rm = new RingMasterClient(serverAddress, null, null, 10000))
                {
                    await rm.Create($"/{RootName}/child1", null, null, CreateMode.PersistentAllowPathCreation);
                    await rm.Create($"/{RootName}/child2", null, null, CreateMode.PersistentAllowPathCreation);
                    await rm.Create($"/{RootName}/child3", null, null, CreateMode.PersistentAllowPathCreation);

                    var ops = new List<Op>
                    {
                        Op.Check($"/{RootName}", -1),
                        Op.GetChildren($"/{RootName}"),
                    };

                    var multiResult = await rm.Multi(ops);
                    Assert.AreEqual(multiResult.Count, ops.Count);

                    var checkResult = multiResult[0] as OpResult.CheckResult;
                    var getChildrenResult = multiResult[1] as OpResult.GetChildrenResult;

                    Assert.AreEqual(3, checkResult.Stat.NumChildren);
                    Assert.AreEqual(RingMasterException.Code.Ok, getChildrenResult.ErrCode);
                    Assert.AreEqual(3, getChildrenResult.Children.Count);
                    Assert.AreEqual(3, getChildrenResult.Stat.NumChildren);
                }
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
                backend.Start(CancellationToken.None);
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

        private async Task DeleteChild(string root, int childCount)
        {
            using (var client = new RingMasterClient(serverAddress, null, null, 10000))
            {
                for (int count = 0; count < childCount; count++)
                {
                    var path = $"/{root}/{count}";
                    await client.Delete(path, -1, DeleteMode.SuccessEvenIfNodeDoesntExist);
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
            using (var client = new RingMasterClient(serverAddress, null, null, 10000))
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
                            log($"FAIL in {threadId}: {ex.Message}");
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
