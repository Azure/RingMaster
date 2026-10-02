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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Vega.Test.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

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

        private static IConfiguration appSettings;

        /// <summary>
        /// Start the backend server
        /// </summary>
        /// <param name="context">Test context</param>
        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            appSettings = builder.Build();

            Helpers.SetupTraceLog(Path.Combine(appSettings["LogFolder"], "VegaInMemoryPerf.LogPath"));
            if (context.GetType().Name.StartsWith("Dummy"))
            {
                log = s => context.WriteLine(s);
            }
            else
            {
                log = s => Trace.TraceInformation(s);
            }

            // If a parameter is specified as follows:
            //      te.exe VegaInMemoryPerf.dll /p:ServerAddress=127.0.0.1:99
            if (!context.Properties.Contains("ServerAddress"))
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

                serverTransport.StartServer(10010);
                serverAddress = "127.0.0.1:10010";
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

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(10 * 1000, cancel);
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
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestGetFullSubtreeWhileUpdating()
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

        /// <summary>
        /// Tests the scenario that multiple threads
        /// try to delete the same node at the same time.
        /// Should not throw any exception.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestConcurrentDelete()
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

        /// <summary>
        /// Checks if conflicting write and read requests on the same node cause any race condition. Exists and
        /// SortedArrayList used to be broken with sporadic exceptions.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestConflictingCreateDeleteExists()
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

            // Running the stress test for 10 minutes.
            SpinWait.SpinUntil(() => clock.Elapsed.TotalSeconds >= 600 || stop);

            stop = true;
            await Task.WhenAll(taskW, taskR).ContinueWith(t => log(t.Exception?.ToString()));
        }

        /// <summary>
        /// This test verifies that the number of children remains the same after failed Multi.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestWrongChildrenCountAfterFailedMulti()
        {
            const string path = "/$rmbvt/test";
            var stop = false;

            // Create a parent node with 3 children. During the test, the number of children is not expected
            // to change.
            using (var rm = new RingMasterClient(serverAddress, null, null, 100000))
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
                using (var rm = new RingMasterClient(serverAddress, null, null, 100000))
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

            // Limiting the test execution to 10 minutes.
            while (clock.Elapsed.TotalMinutes < 10)
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

        /// <summary>
        /// This test verifies that the requests in multi will return proper response
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestRequestInMultiReturnCorrectly()
        {
            const string RootName = nameof(this.TestRequestInMultiReturnCorrectly);
            using (var rm = new RingMasterClient(serverAddress, null, null, 100000))
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

        /// <summary>
        /// Tests the watcher delivered with child's change
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestWatcherDeliverWithChildChange()
        {
            const string RootName = nameof(this.TestWatcherDeliverWithChildChange);
            var sw = Stopwatch.StartNew();
            int eventCount = 0;
            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                await client.Create($"/{RootName}/a/b/child1", null, null, CreateMode.PersistentAllowPathCreation);
                var watcher = new CallbackWatcher
                {
                    Kind = WatcherKind.IncludeDataAndChildChange,
                    OnProcess = (watchedEvent) =>
                    {
                        if (watchedEvent.EventType != WatchedEvent.WatchedEventType.WatcherRemoved)
                        {
                            log(watchedEvent.ToString());
                            Assert.AreEqual(WatchedEvent.WatchedEventType.NodeChildrenChanged, watchedEvent.EventType);
                            Assert.AreEqual($"/{RootName}/a/b", watchedEvent.Path);
                            Assert.AreEqual("child1", watchedEvent.ChildName);
                            Assert.IsTrue(watchedEvent.Stat != null);
                            Interlocked.Increment(ref eventCount);
                        }
                    },
                };

                await client.RegisterBulkWatcher($"/{RootName}", watcher).ConfigureAwait(false);

                // await client.Exists($"/{RootName}/a/b/child1", watcher);
                // Thread.Sleep(5 * 1000);
                await client.Delete($"/{RootName}/a/b/child1", -1, DeleteMode.None);

                await Task.Delay(2 * 1000);

                await client.Create($"/{RootName}/a/b/child1", null, null, CreateMode.Persistent);
            }

            // wait until we receive the watcher events
            await Task.Delay(5 * 1000);
            Assert.AreEqual(2, eventCount);
        }

        /// <summary>
        /// Tests the watcher delivered correctly.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestWatcherDeliveredCorrectly()
        {
            const string RootName = nameof(this.TestWatcherDeliveredCorrectly);
            var countDown = new CountdownEvent(2);

            bool nodeDeleteReceived = false;
            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                await client.Create($"/{RootName}/a/child1", null, null, CreateMode.PersistentAllowPathCreation);
                var watcher = new CallbackWatcher
                {
                    Kind = 0,
                    OnProcess = (watchedEvent) =>
                    {
                        if (watchedEvent.EventType == WatchedEvent.WatchedEventType.WatcherRemoved)
                        {
                            return;
                        }

                        log(watchedEvent.ToString());

                        if (watchedEvent.EventType == WatchedEvent.WatchedEventType.NodeChildrenChanged)
                        {
                            Assert.IsTrue(nodeDeleteReceived);
                            countDown.Signal();
                        }
                        else if (watchedEvent.EventType == WatchedEvent.WatchedEventType.NodeDeleted)
                        {
                            nodeDeleteReceived = true;
                            Assert.AreEqual($"/{RootName}/a/child1", watchedEvent.Path);
                            countDown.Signal();
                        }
                    },
                };

                await client.RegisterBulkWatcher($"/{RootName}", watcher).ConfigureAwait(false);

                await client.Delete($"/{RootName}/a/child1", -1, DeleteMode.CascadeDelete);
            }

            // wait until we receive the watcher events
            Assert.IsTrue(countDown.Wait(5 * 1000));
            Assert.IsTrue(nodeDeleteReceived);
        }

        /// <summary>
        /// Tests the session's on terminate actions. Specifically, when the remove watcher action failed (due to not able to acquire writer lock)
        /// The remove ephemeral nodes should not be impacted.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestOnSessionTerminateActions()
        {
            const string RootName = nameof(this.TestOnSessionTerminateActions);
            const string EphemeralRoot = nameof(EphemeralRoot);
            int threadCount = 8;

            int watcherReceivedNum = 0;
            var watcher = new CallbackWatcher
            {
                OnProcess = (watchedEvent) =>
                {
                    if (watchedEvent.EventType == WatchedEvent.WatchedEventType.WatcherRemoved && watchedEvent.KeeperState == WatchedEvent.WatchedEventKeeperState.Disconnected)
                    {
                        Interlocked.Increment(ref watcherReceivedNum);
                    }
                },
            };

            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                await client.Create($"/{RootName}", null, null, CreateMode.PersistentAllowPathCreation);
                await client.Create($"/{EphemeralRoot}", null, null, CreateMode.PersistentAllowPathCreation);
            }

            var clock = Stopwatch.StartNew();
            while (clock.Elapsed.TotalSeconds <= 60)
            {
                try
                {
                    var allTask = new List<Task>();
                    for (int i = 0; i < threadCount; i++)
                    {
                        allTask.Add(Task.Run(async () =>
                        {
                            using (var rm = new RingMasterClient(serverAddress, null, null, 100000))
                            {
                                await rm.Exists($"/{RootName}", watcher);
                                await rm.Create($"/{EphemeralRoot}/{Guid.NewGuid()}", null, null, CreateMode.Ephemeral);
                            }
                        }));
                    }

                    await Task.WhenAll(allTask);
                }
                catch (Exception ex)
                {
                    log("Exception when creating watchers: " + ex.ToString());
                    throw;
                }
            }

            await Task.Delay(2000);
            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                var children = await client.GetChildren($"/{EphemeralRoot}", null);
                log($"children count: {children.Count}");
                foreach (var child in children)
                {
                    log(child);
                }

                // TODO: Enable below line.
                // Assert.IsTrue(children.Count == 0);
            }
        }

        /// <summary>
        /// Tests the system information multi thread.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestSystemInfoMultiThread()
        {
            int writeTaskCount = 1000;
            int readTaskCount = 5000;
            int failTaskCount = 200;

            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                var stat = await client.Exists("/", null);

                var initBytes = await client.GetData(SystemInfo.SystemInfoPath, null);
                log(SystemInfo.Deserialize(initBytes).ToString());
            }

            var writeTask = Task.Run(async () =>
            {
                using (var client = new RingMasterClient(serverAddress, null, null, 100000))
                {
                    for (int i = 0; i < writeTaskCount; i++)
                    {
                        await client.Create($"/{Guid.NewGuid()}", null, null, CreateMode.Persistent);
                    }
                }
            });

            var readTask = Task.Run(async () =>
            {
                using (var client = new RingMasterClient(serverAddress, null, null, 100000))
                {
                    for (int i = 0; i < readTaskCount; i++)
                    {
                        await client.Exists("/", null);
                    }
                }
            });

            var failTask = Task.Run(async () =>
            {
                using (var client = new RingMasterClient(serverAddress, null, null, 100000))
                {
                    for (int i = 0; i < failTaskCount; i++)
                    {
                        try
                        {
                            await client.Exists($"/abc", null);
                        }
                        catch (RingMasterException)
                        {
                        }
                    }
                }
            });

            await Task.WhenAll(writeTask, readTask, failTask);

            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                var bytes = await client.GetData(SystemInfo.SystemInfoPath, null);

                var systemInfo = SystemInfo.Deserialize(bytes);
                log(systemInfo.ToString());

                long receivedRequestCount = systemInfo.GetAllRequestCount();
                Assert.IsTrue(receivedRequestCount > (writeTaskCount + readTaskCount + failTaskCount));
                Assert.IsTrue(receivedRequestCount >= systemInfo.RespondedRequestCount);
                Assert.IsTrue(receivedRequestCount - systemInfo.RespondedRequestCount < 10);
                Assert.IsTrue(systemInfo.ErrorCodeDistribution[(int)RingMasterException.Code.Nonode] >= failTaskCount);
            }
        }

        /// <summary>
        /// Tests requests' error path.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        [Ignore("This will impact other tests.")]
        [Timeout(5000)]
        public async Task TestRequestErrorPath()
        {
            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                var path = Guid.NewGuid().ToString();
                var r1 = await client.Create($"/{path}", null, null, CreateMode.PersistentAllowPathCreation);
                Assert.AreEqual(path, r1);

                inMemoryFactory.Deactivate();
                try
                {
                    var s2 = await client.Create($"/{Guid.NewGuid()}", null, null, CreateMode.PersistentAllowPathCreation);
                }
                catch (RingMasterException ex)
                {
                    Assert.AreEqual(ex.ErrorCode, RingMasterException.Code.OperationCancelled);
                }
            }
        }

        /// <summary>
        /// Tests the watcher perf.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestWatcherPerf()
        {
            int childCount = 32000;
            long watcherReceivedNum = 0;
            var watcher = new CallbackWatcher
            {
                OnProcess = (watchedEvent) =>
                {
                    if (watchedEvent.EventType != WatchedEvent.WatchedEventType.WatcherRemoved)
                    {
                        Interlocked.Increment(ref watcherReceivedNum);
                    }
                },
            };

            var clock = Stopwatch.StartNew();
            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                var path = Guid.NewGuid().ToString();
                var r1 = await client.Create($"/{path}", null, null, CreateMode.PersistentAllowPathCreation);

                List<Op> createChildren = new List<Op>();
                for (int i = 0; i < childCount; i++)
                {
                    createChildren.Add(Op.Create($"/{path}/{i}", null, null, CreateMode.PersistentAllowPathCreation));
                }

                log($"creating {childCount} children");
                await client.Multi(createChildren);
                log($"all children created");

                await client.RegisterBulkWatcher($"/{path}", watcher);

                TimeSpan startTime = TimeSpan.FromDays(10);
                var unused = Task.Run(async () =>
                {
                    using (var client2 = new RingMasterClient(serverAddress, null, null, 100000))
                    {
                        List<Op> updateChildren = new List<Op>();
                        for (int i = 0; i < childCount; i++)
                        {
                            updateChildren.Add(Op.SetData($"/{path}/{i}", Guid.NewGuid().ToByteArray(), -1));
                        }

                        startTime = clock.Elapsed;
                        await client2.Multi(updateChildren);
                    }
                });

                var unused2 = Task.Run(() =>
                {
                    while (Interlocked.Read(ref watcherReceivedNum) < childCount)
                    {
                        Thread.Yield();
                    }

                    log($"Received all watcher {(clock.Elapsed - startTime).TotalMilliseconds}");
                });

                await Task.Delay(5000);
            }
        }

        /// <summary>
        /// Tests the set user metadata.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestSetDataAndUserMetadata()
        {
            var getDataMetadataAndStatOption = RequestDefinitions.RequestGetData.GetDataOptions.UserMetadataRequired;
            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                var path = $"/{nameof(this.TestSetDataAndUserMetadata)}{Guid.NewGuid().ToString()}";
                var metadataString = "This is user metadata";
                byte[] metadata = Encoding.ASCII.GetBytes(metadataString);
                var initStat = await client.CreateAndGetStat(path, Guid.NewGuid().ToByteArray(), null, CreateMode.PersistentAllowPathCreation, metadata);
                Assert.AreEqual(1, initStat.Uversion);
                var retrievedValue = await client.GetData(path, getDataMetadataAndStatOption, null);
                var retrivedMetadataString = Encoding.ASCII.GetString(retrievedValue.UserMetadata);
                Assert.AreEqual(metadataString, retrivedMetadataString);

                var stat = await client.SetDataAndUserMetadata(path, null, -1, null, 1);
                Assert.AreEqual(2, stat.Uversion);
                Assert.AreEqual(2, stat.Version);

                retrievedValue = await client.GetData(path, getDataMetadataAndStatOption, null);
                Assert.IsTrue(retrievedValue.Stat.Equals(stat));
                Assert.IsNull(retrievedValue.Data);
                Assert.IsNull(retrievedValue.UserMetadata);

                var metadataString2 = "new user metadata version!";
                byte[] newMetadata = Encoding.ASCII.GetBytes(metadataString2);
                await client.CreateAndGetStat(path, null, null, CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag, userMetadata: newMetadata);
                var retrievedValue2 = await client.GetData(path, getDataMetadataAndStatOption, null);
                Assert.AreEqual(3, retrievedValue2.Stat.Uversion);
                Assert.AreEqual(3, retrievedValue2.Stat.Version);
                Assert.IsNull(retrievedValue2.Data);
                var retrivedMetadataString2 = Encoding.ASCII.GetString(retrievedValue2.UserMetadata);
                Assert.AreEqual(metadataString2, retrivedMetadataString2);

                var newData = "New data on parent!";
                await client.CreateAndGetStat(path, Encoding.ASCII.GetBytes(newData), null, CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag);
                var retrievedValue3 = await client.GetData(path, getDataMetadataAndStatOption, null);
                Assert.AreEqual(4, retrievedValue3.Stat.Uversion);
                Assert.IsNull(retrievedValue3.UserMetadata);
                Assert.IsNotNull(retrievedValue3.Data);

                List<Op> ops = new List<Op>();
                ops.Add(Op.Create($"{path}/c1", null, null, CreateMode.PersistentAllowPathCreation, Encoding.ASCII.GetBytes($"{path}/c1")));
                ops.Add(Op.SetDataAndUserMetadata(path, Encoding.ASCII.GetBytes(newData), retrievedValue3.Stat.Version, Encoding.ASCII.GetBytes(path), retrievedValue3.Stat.Uversion));
                ops.Add(Op.GetData(path, Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions.UserMetadataRequired, Op.Check(path, -1)));
                ops.Add(Op.GetSubtree(path, ">:256:", Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetSubtree.GetSubtreeOptions.IncludeUserMetadata));

                var multiResult = await client.Multi(ops);
                var result0 = multiResult[0] as OpResult.CreateResult;
                Assert.AreEqual(1, result0.Stat.Uversion);
                var result1 = multiResult[1] as OpResult.SetDataAndUserMetadataResult;
                Assert.AreEqual(5, result1.Stat.Uversion);
                Assert.AreEqual(5, result1.Stat.Version);
                var result2 = multiResult[2] as OpResult.GetDataResult;
                Assert.IsNotNull(result2.Data);
                Assert.IsNotNull(result2.UserMetadata);
                Assert.IsNotNull(result2.Stat);
                var result3 = multiResult[3] as OpResult.GetSubtreeResult;
                var treeNode = TreeNode.Deserialize(result3.SerializedSubtree);
                Assert.AreEqual(newData, Encoding.ASCII.GetString(treeNode.Data));
                Assert.IsNull(treeNode.Stat);
                Assert.AreEqual(path, Encoding.ASCII.GetString(treeNode.UserMetadata));
            }
        }

        /// <summary>
        /// The ringmaster client backward compatibility test. This test uses new version of RingMasterClient (after
        /// the user metadata change) but target to older server (without user metadata change).
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestClientBackwardCompatible()
        {
            var oldServer = serverAddress; // "127.0.0.1:33541";
            var rootPath = $"/{Guid.NewGuid()}";
            var rootData = Guid.NewGuid().ToByteArray();
            using (var client = new RingMasterClient(oldServer, null, null, 5000))
            {
                await client.Create(rootPath, rootData, null, CreateMode.PersistentAllowPathCreation);
                VerifyStatForFreshlyCreatedNode(await client.Exists(rootPath, null));
                var childData1 = Guid.NewGuid().ToByteArray();
                var childData2 = Guid.NewGuid().ToByteArray();

                await client.Create($"{rootPath}/child1", childData1, null, CreateMode.PersistentAllowPathCreation);
                await client.Create($"{rootPath}/child2", childData2, null, CreateMode.PersistentAllowPathCreation);

                VerifyBytesAreEqual(rootData, await client.GetData(rootPath, null));
                VerifyBytesAreEqual(childData1, await client.GetData($"{rootPath}/child1", null));
                VerifyBytesAreEqual(childData2, await client.GetData($"{rootPath}/child2", null));

                var getDataResponse = await client.GetData(rootPath, RequestDefinitions.RequestGetData.GetDataOptions.None, null);
                VerifyBytesAreEqual(rootData, getDataResponse.Data);
                Assert.IsNull(getDataResponse.UserMetadata);
                Assert.IsNotNull(getDataResponse.Stat);
                Assert.AreEqual(2, getDataResponse.Stat.NumChildren);
                Assert.AreEqual(1, getDataResponse.Stat.Version);

                var subtree = await client.GetFullSubtree(rootPath, RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.IncludeStats);
                VerifyBytesAreEqual(rootData, subtree.Data);
                VerifyBytesAreEqual(childData1, subtree.Children[0].Data);
                VerifyBytesAreEqual(childData2, subtree.Children[1].Data);
                VerifyStatForFreshlyCreatedNode(subtree.Children[0].Stat);
                VerifyStatForFreshlyCreatedNode(subtree.Children[1].Stat);

                subtree = await client.GetFullSubtree(rootPath, RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.None);
                VerifyBytesAreEqual(rootData, subtree.Data);
                VerifyBytesAreEqual(childData1, subtree.Children[0].Data);
                VerifyBytesAreEqual(childData2, subtree.Children[1].Data);
                Assert.IsNull(subtree.Stat);
                Assert.IsNull(subtree.Children[0].Stat);
                Assert.IsNull(subtree.Children[1].Stat);

                // test batch/multi
                var ops = new List<Op>();
                ops.Add(Op.Create($"{rootPath}/child3", Guid.NewGuid().ToByteArray(), null, CreateMode.PersistentAllowPathCreation, null));
                ops.Add(Op.SetData(rootPath, Guid.NewGuid().ToByteArray(), -1));
                ops.Add(Op.GetData($"{rootPath}/child1", RequestDefinitions.RequestGetData.GetDataOptions.None, null));
                ops.Add(Op.GetData($"{rootPath}/child2", RequestDefinitions.RequestGetData.GetDataOptions.NoStatRequired, null));

                var multiResults = await client.Multi(ops);
                var createResult = multiResults[0] as OpResult.CreateResult;
                Assert.AreEqual(OpCode.Create, createResult.ResultType);
                Assert.AreEqual(RingMasterException.Code.Ok, createResult.ErrCode);
                Assert.AreEqual($"child3", createResult.Path);

                var setResult = multiResults[1] as OpResult.SetDataResult;
                Assert.AreEqual(OpCode.SetData, setResult.ResultType);
                Assert.AreEqual(RingMasterException.Code.Ok, setResult.ErrCode);
                Assert.IsNotNull(setResult.Stat);

                var getResult = multiResults[2] as OpResult.GetDataResult;
                Assert.AreEqual(OpCode.GetData, getResult.ResultType);
                Assert.AreEqual(RingMasterException.Code.Ok, getResult.ErrCode);
                Assert.AreEqual(1, getResult.Stat.Version);
                Assert.IsNull(getResult.UserMetadata);
                VerifyBytesAreEqual(childData1, getResult.Data);

                var getResultNoStat = multiResults[3] as OpResult.GetDataResult;
                Assert.AreEqual(OpCode.GetData, getResultNoStat.ResultType);
                Assert.AreEqual(RingMasterException.Code.Ok, getResultNoStat.ErrCode);
                Assert.IsNull(getResultNoStat.Stat);
                VerifyBytesAreEqual(childData2, getResultNoStat.Data);
                Assert.IsNull(getResultNoStat.UserMetadata);

                Assert.IsTrue(await client.Delete(rootPath, -1, DeleteMode.CascadeDelete));
            }
        }

        /// <summary>
        /// Tests the get data not null stree.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestGetDataAfterCreateStress()
        {
            var testTime = TimeSpan.FromMinutes(1);
            int createClientCount = threadCount;
            var root = Guid.NewGuid().ToString();
            long receivedEventCount = 0;
            int createdChild = 0;
            int getClientCount = int.Parse(appSettings["TestGetDataAfterCreateStress.GetThreadCount"]);
            ManualResetEvent canCreateChildren = new ManualResetEvent(false);
            var clock = Stopwatch.StartNew();

            var getClients = Enumerable.Range(0, getClientCount).Select(x => new RingMasterClient(serverAddress, null, null, 100000)).ToArray();
            long totalNumberOfGets = 0;

            var showStatus = Task.Run(async () =>
            {
                while (clock.Elapsed < testTime)
                {
                    await Task.Delay(1000);
                    log($"created child count: {createdChild}, received events: {Interlocked.Read(ref receivedEventCount)}. Total Number of Get: {Interlocked.Read(ref totalNumberOfGets)}");
                }
            });

            var watcherTask = Task.Run(async () =>
            {
                using (var client1 = new RingMasterClient(serverAddress, null, null, 100000))
                {
                    using (var client2 = new RingMasterClient(serverAddress, null, null, 100000))
                    {
                        await client1.Create($"/{root}/parent", null, null, CreateMode.PersistentAllowPathCreation);
                        var watcher = new CallbackWatcher
                        {
                            OnProcess = async (watchedEvent) =>
                            {
                                if (watchedEvent.EventType == WatchedEvent.WatchedEventType.NodeCreated)
                                {
                                    try
                                    {
                                        var tasks = Enumerable.Range(0, getClientCount)
                                            .Select(n => getClients[n].GetData(watchedEvent.Path, null)
                                                    .ContinueWith(t =>
                                                    {
                                                        if (t.Result == null || t.Exception != null)
                                                        {
                                                            log($"Exception when getting data on {watchedEvent.Path}");
                                                            Environment.Exit(-1);
                                                        }
                                                    }))
                                            .ToArray();

                                        await Task.WhenAll(tasks);
                                        Interlocked.Add(ref totalNumberOfGets, getClientCount);
                                        Interlocked.Increment(ref receivedEventCount);

                                        Assert.IsTrue(await client2.Delete(watchedEvent.Path, -1));
                                    }
                                    catch (RingMasterException e)
                                    {
                                        log($"Exception when processing event {watchedEvent.EventType}, {watchedEvent.Path}, {watchedEvent.ChildName}. CreatedChild: {createdChild}, watcherReceived: {receivedEventCount}");
                                        log(e.ErrorCode.ToString());
                                        Environment.Exit(-1);
                                    }
                                    catch (Exception ex)
                                    {
                                        log($"Exception when processing event {watchedEvent.EventType}, {watchedEvent.Path}, {watchedEvent.ChildName}. CreatedChild: {createdChild}, watcherReceived: {receivedEventCount}");
                                        log(ex.ToString());
                                        Environment.Exit(-1);
                                    }
                                }
                            },
                        };

                        await client1.RegisterBulkWatcher($"/{root}", watcher);
                        canCreateChildren.Set();
                        await Task.Delay(testTime);

                        await Task.Delay(5000);
                    }
                }
            });

            var threads = Enumerable.Range(0, createClientCount)
                .Select(n => new Thread(async () =>
                {
                    using (var client = new RingMasterClient(serverAddress, null, null, 100000))
                    {
                        canCreateChildren.WaitOne();
                        int i = 0;
                        while (clock.Elapsed < testTime)
                        {
                            await client.Create($"/{root}/parent/child_{n}_{i++}", Guid.NewGuid().ToByteArray(), null, CreateMode.PersistentAllowPathCreation);
                            Interlocked.Increment(ref createdChild);
                        }
                    }
                })).ToArray();

            var startTime = clock.Elapsed;
            Parallel.ForEach(threads, t => t.Start());
            await Task.Delay(testTime);

            Parallel.ForEach(threads, t => t.Join());
            var elapsed = clock.Elapsed - startTime;

            log($"final created child count: {createdChild}, rate: {createdChild / elapsed.TotalSeconds:G4}/sec, received events: {Interlocked.Read(ref receivedEventCount)}, " +
                $"rate {Interlocked.Read(ref receivedEventCount) / elapsed.TotalSeconds:G4}/sec, " +
                $"Total Number of Get: {Interlocked.Read(ref totalNumberOfGets)}, rate: {Interlocked.Read(ref totalNumberOfGets) / elapsed.TotalSeconds:G4}/sec. " +
                $"Get thread count: {getClientCount}");
        }

        /// <summary>
        /// Tests the get data after set stress.
        /// </summary>
        /// <returns>async Task</returns>
        [TestMethod]
        [Ignore]
        public async Task TestGetDataAfterSetStress()
        {
            int testTimeSeconds = 100;
            int createClientCount = threadCount;
            var root = Guid.NewGuid().ToString();
            long receivedEventCount = 0;
            int getClientCount = int.Parse(appSettings["TestGetDataAfterCreateStress.GetThreadCount"]);
            ManualResetEvent canUpdateChild = new ManualResetEvent(false);
            var clock = Stopwatch.StartNew();

            var getClients = Enumerable.Range(0, getClientCount).Select(x => new RingMasterClient(serverAddress, null, null, 10000)).ToArray();
            long totalNumberOfGets = 0;
            long updatedCount = 0;
            long totalCreated = 0;

            var threads = Enumerable.Range(0, createClientCount)
                .Select(n => new Thread(async () =>
                {
                    using (var client = new RingMasterClient(serverAddress, null, null, 10000))
                    {
                        int i = 0;
                        while (clock.Elapsed < TimeSpan.FromSeconds(testTimeSeconds))
                        {
                            await client.Create($"/{root}/parent/child_{n}_{i++}", null, null, CreateMode.PersistentAllowPathCreation);
                            Interlocked.Increment(ref totalCreated);
                        }
                    }
                })).ToArray();

            log("Begin creating nodes");
            Parallel.ForEach(threads, t => t.Start());
            await Task.Delay(TimeSpan.FromSeconds(testTimeSeconds));
            Parallel.ForEach(threads, t => t.Join());
            log($"Create all nodes finished, total number created: {totalCreated}");

            clock.Reset();
            var showStatus = Task.Run(async () =>
            {
                while (clock.Elapsed < TimeSpan.FromSeconds(testTimeSeconds + 10))
                {
                    await Task.Delay(1000);
                    log($"updated count: {Interlocked.Read(ref updatedCount)} received events: {Interlocked.Read(ref receivedEventCount)}. Total Number of Get: {Interlocked.Read(ref totalNumberOfGets)}");
                }
            });

            var watcherTask = Task.Run(async () =>
            {
                using (var client1 = new RingMasterClient(serverAddress, null, null, 10000))
                {
                    using (var client2 = new RingMasterClient(serverAddress, null, null, 10000))
                    {
                        var watcher = new CallbackWatcher
                        {
                            OnProcess = async (watchedEvent) =>
                            {
                                // log($" processing event {watchedEvent.EventType}, {watchedEvent.Path}, {watchedEvent.ChildName}");
                                if (watchedEvent.EventType == WatchedEvent.WatchedEventType.NodeDataChanged)
                                {
                                    try
                                    {
                                        var tasks = Enumerable.Range(0, getClientCount)
                                            .Select(n => getClients[n].GetData(watchedEvent.Path, null)
                                                    .ContinueWith(t =>
                                                    {
                                                        if (t.Result == null || t.Exception != null)
                                                        {
                                                            log($"Exception when getting data on {watchedEvent.Path}");
                                                            Environment.Exit(-1);
                                                        }
                                                    }))
                                            .ToArray();

                                        await Task.WhenAll(tasks);
                                        Interlocked.Add(ref totalNumberOfGets, getClientCount);
                                        Interlocked.Increment(ref receivedEventCount);

                                        Assert.IsTrue(await client2.Delete(watchedEvent.Path, -1));
                                    }
                                    catch (RingMasterException e)
                                    {
                                        log($"Exception when processing event {watchedEvent.EventType}, {watchedEvent.Path}, {watchedEvent.ChildName}. UpdatedChild: {updatedCount}, watcherReceived: {receivedEventCount}");
                                        log(e.ErrorCode.ToString());
                                        Environment.Exit(-1);
                                    }
                                    catch (Exception ex)
                                    {
                                        log($"Exception when processing event {watchedEvent.EventType}, {watchedEvent.Path}, {watchedEvent.ChildName}. UpdatedChild: {updatedCount}, watcherReceived: {receivedEventCount}");
                                        log(ex.ToString());
                                        Environment.Exit(-1);
                                    }
                                }
                            },
                        };

                        await client1.RegisterBulkWatcher($"/{root}", watcher);
                        canUpdateChild.Set();
                        await Task.Delay(TimeSpan.FromSeconds(testTimeSeconds + 10));
                    }
                }
            });

            var updateThreads = Enumerable.Range(0, createClientCount)
                .Select(n => new Thread(async () =>
                {
                    using (var client = new RingMasterClient(serverAddress, null, null, 10000))
                    {
                        int i = 0;
                        while (clock.Elapsed < TimeSpan.FromSeconds(testTimeSeconds / 2))
                        {
                            await client.SetData($"/{root}/parent/child_{n}_{i++}", Guid.NewGuid().ToByteArray(), -1);
                            Interlocked.Increment(ref updatedCount);
                        }
                    }
                })).ToArray();

            log("Begin updating nodes");
            Parallel.ForEach(updateThreads, t => t.Start());
            await Task.Delay(TimeSpan.FromSeconds(testTimeSeconds / 2));
            Parallel.ForEach(updateThreads, t => t.Join());

            log($"Total Number of Get: {Interlocked.Read(ref totalNumberOfGets)}");
        }

        /// <summary>
        /// Tests the get children from same parent perf.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestGetChildrenFromSameParentPerf()
        {
            int childCount = 100;
            var parentPath = $"/{Guid.NewGuid().ToString()}/d/e/parent";
            using (var client1 = new RingMasterClient(serverAddress, null, null, 100000))
            {
                for (int i = 0; i < childCount; i++)
                {
                    await client1.Create($"{parentPath}/{i}", null, null, CreateMode.PersistentAllowPathCreation);
                }
            }

            Random rnd = new Random();
            int getThreadCount = threadCount;
            var testTime = TimeSpan.FromSeconds(60);
            var clock = Stopwatch.StartNew();
            long getCount = 0;
            List<long> diffs = new List<long>();
            long deletedCount = 0;
            var showStatus = Task.Run(async () =>
            {
                long lastCount = 0;
                while (clock.Elapsed < testTime)
                {
                    await Task.Delay(1000);
                    var currentCount = Interlocked.Read(ref getCount);
                    long diff = currentCount - lastCount;
                    log($"Total Number of Get: {currentCount}, diff {diff}. Deleted: {Interlocked.Read(ref deletedCount)}");
                    diffs.Add(diff);
                    lastCount = currentCount;
                }
            });

            /*var deleteTask = Task.Run(async () =>
            {
                using (var client = new RingMasterClient(serverAddress, null, null, 10000))
                {
                    try
                    {
                        while (clock.Elapsed < testTime)
                        {
                            var child = Guid.NewGuid().ToString();
                            await client.Create($"{parentPath}/{child}", null, null, CreateMode.PersistentAllowPathCreation);
                            Assert.IsTrue(await client.Delete($"{parentPath}/{child}", -1, DeleteMode.None));
                            Interlocked.Increment(ref deletedCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        log($"Exp: {ex.ToString()}");
                    }
                }
            });*/

            int asyncTaskCount = 64;
            int taskCount = 0;
            var getThreads = Enumerable.Range(0, getThreadCount)
                .Select(n => new Thread(() =>
                {
                    using (var client = new RingMasterClient(serverAddress, null, null, 100000))
                    {
                        while (clock.Elapsed < testTime)
                        {
                            SpinWait.SpinUntil(() => taskCount < asyncTaskCount);
                            int index = rnd.Next(childCount);
                            var task = client.GetData($"{parentPath}/{index}", false)
                                .ContinueWith(t =>
                                {
                                    Interlocked.Decrement(ref taskCount);
                                });

                            Interlocked.Increment(ref taskCount);
                            Interlocked.Increment(ref getCount);
                        }
                    }
                })).ToArray();

            var startTime = clock.Elapsed;
            Parallel.ForEach(getThreads, t => t.Start());
            await Task.Delay(testTime - TimeSpan.FromSeconds(5));

            Parallel.ForEach(getThreads, t => t.Join());
            var elapsed = clock.Elapsed - startTime;
            var totalGet = Interlocked.Read(ref getCount);
            diffs.RemoveAll(x => x < 1000);
            log($"Total Get: {totalGet}, rate {totalGet / elapsed.TotalSeconds} / second, avg diff per second: {diffs.Average()}");
        }

        /// <summary>
        /// Tests the large object heap buildup.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestLargeObjectHeapBuildup()
        {
            var rnd = new Random();
            var largeTreeRoot = Guid.NewGuid().ToString();
            int subtreeSize = int.Parse(appSettings["TestLargeObjectHeapBuildup.SubtreeSize"]);
            using (var client = new RingMasterClient(serverAddress, null, null, 100000))
            {
                for (int t = 0; t < threadCount; t++)
                {
                    for (int i = 0; i < subtreeSize; i++)
                    {
                        var data = new byte[1024];
                        rnd.NextBytes(data);
                        await client.Create($"/{largeTreeRoot}/thread_{t}/{i}", data, null, CreateMode.PersistentAllowPathCreation);
                    }
                }
            }

            log($"created tree count {threadCount} with subtree size {subtreeSize}");

            var clock = Stopwatch.StartNew();
            var testTime = TimeSpan.FromSeconds(30);
            int taskCount = 0;
            int asyncTaskCount = 8;
            var getThreads = Enumerable.Range(0, threadCount)
                .Select(n => new Thread(() =>
                {
                    using (var client = new RingMasterClient(serverAddress, null, null, 100000))
                    {
                        while (clock.Elapsed < testTime)
                        {
                            SpinWait.SpinUntil(() => taskCount < asyncTaskCount);
                            var task = client.GetFullSubtree($"/{largeTreeRoot}/thread_{n}")
                                .ContinueWith(t =>
                                {
                                    Interlocked.Decrement(ref taskCount);
                                });

                            Interlocked.Increment(ref taskCount);
                        }
                    }
                })).ToArray();

            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);

            log($"running get full subtree....");
            Parallel.ForEach(getThreads, t => t.Start());
            Parallel.ForEach(getThreads, t => t.Join());

            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void VerifyStatForFreshlyCreatedNode(IStat stat, string context = null)
        {
            // Node exists, so stat should not be null
            Assert.IsNotNull(stat);

            // Node has just been created so Czxid (create transaction id)
            // must be equal to Mzxid (modify transaction id). Similarly,
            // Ctime must be equal to Mtime.
            Assert.AreEqual(stat.Czxid, stat.Mzxid, string.Format("Czxid vs Mzxid {0}", context));
            Assert.AreEqual(stat.Ctime, stat.Mtime, string.Format("Ctime vs Mtime {0}", context));

            // Since no children were added or deleted, Pzxid must be
            // the same as Czxid.
            Assert.AreEqual(stat.Czxid, stat.Pzxid);

            // No Changes yet, so version must be 1.
            Assert.AreEqual(1, stat.Version);
            Assert.AreEqual(1, stat.Cversion);
            Assert.AreEqual(1, stat.Aversion);
        }

        private static void VerifyBytesAreEqual(byte[] expected, byte[] actual, string message = null)
        {
            if (message != null)
            {
                Assert.AreEqual(expected.Length, actual.Length, message);
            }
            else
            {
                Assert.AreEqual(expected.Length, actual.Length);
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (message != null)
                {
                    Assert.AreEqual(expected[i], actual[i], message);
                }
                else
                {
                    Assert.AreEqual(expected[i], actual[i]);
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
