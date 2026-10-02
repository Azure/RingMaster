// <copyright file="CallbackBeforeCompleteTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
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
    using IRingMasterServerInstrumentation = Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.IRingMasterServerInstrumentation;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Test callback before response
    /// </summary>
    [TestClass]
    public class CallbackBeforeCompleteTests
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
            if (context.GetType().Name.StartsWith("Dummy"))
            {
                log = s => context.WriteLine(s);
            }
            else
            {
                log = s => Trace.TraceInformation(s);
            }

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

        /// <summary>
        /// Tests the callback properly called.
        /// </summary>
        /// <returns>async task</returns>
        [TestMethod]
        public async Task TestCallbackProperlyCalled()
        {
            var path = $"/{Guid.NewGuid().ToString()}";
            var data = new byte[16];
            var rnd = new Random();
            rnd.NextBytes(data);

            using (var client = new RingMasterClient(serverAddress, null, null, 10000))
            {
                await client.CreateAndGetStat($"/{Guid.NewGuid()}", data, null, CreateMode.AllowPathCreationFlag, false);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(null, null, null, 0);

                var stat = await client.CreateAndGetStat(path, data, null, CreateMode.PersistentAllowPathCreation, true, userMetadata: data);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(path, data, stat, 1, data);

                await client.GetData(path, null, true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(path, data, null, 2);

                var newData = new byte[8];
                rnd.NextBytes(newData);
                var newStat = await client.SetData(path, newData, -1, true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(path, newData, newStat, 3);

                // exists request should not call callback.
                await client.Exists(path, null);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(null, null, null, 3);

                var dataWithStat = await client.GetData(path, RequestDefinitions.RequestGetData.GetDataOptions.None, null, true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(path, dataWithStat.Data, dataWithStat.Stat, 4);

                var dataWithStatAndMetadata = await client.GetData(path, RequestDefinitions.RequestGetData.GetDataOptions.UserMetadataRequired, null, true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(path, dataWithStatAndMetadata.Data, dataWithStatAndMetadata.Stat, 5, dataWithStatAndMetadata.UserMetadata);

                newStat = await client.SetDataAndUserMetadata(path, newData, -1, newData, -1, true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(path, newData, newStat, 6, newData);

                await client.Delete(path, -1, invokeCallbackBeforeComplete: true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(path, null, null, 7);

                var ops = new List<Op>();
                ops.Add(Op.Create($"{path}/1", data, null, CreateMode.AllowPathCreationFlag));
                ops.Add(Op.GetData($"{path}/1", Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions.None, null));
                ops.Add(Op.SetData($"{path}/1", data, -1));
                ops.Add(Op.Exists($"{path}/1"));

                var result = await client.Multi(ops, invokeCallbackBeforeComplete: true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation($"{path}/1", data, ((OpResult.SetDataResult)result[2]).Stat, 10);

                // failed request should not call callback.
                await client.Delete($"/{Guid.NewGuid()}", -1, DeleteMode.None, invokeCallbackBeforeComplete: true);
                this.AssertCallbackCalledCorrectlyAndResetInstrumentation(null, null, null, 10);
            }
        }

        private static RingMasterBackendCore CreateBackend()
        {
            RingMasterBackendCore backend = null;
            try
            {
                var backendStarted = new ManualResetEventSlim();

                backend = new RingMasterBackendCore(inMemoryFactory, serverInstrumentation: TestServerInstrumentation.Instance);

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

        private void AssertCallbackCalledCorrectlyAndResetInstrumentation(string expectedPath, byte[] expectedData, IStat expectedStat, int totalCalledCount, byte[] expectedUserMetadata = null)
        {
            var instrumentation = (TestServerInstrumentation)TestServerInstrumentation.Instance;
            Assert.AreEqual(totalCalledCount, instrumentation.CalledCount);
            Assert.AreEqual(expectedPath, instrumentation.Path);
            Assert.IsTrue((expectedData == null && instrumentation.Data == null) || (expectedData != null && instrumentation.Data != null && expectedData.SequenceEqual(instrumentation.Data)));
            Assert.IsTrue((expectedStat == null && instrumentation.Stat == null) || (expectedStat != null && instrumentation.Stat != null && expectedStat.Equals(instrumentation.Stat)));
            Assert.IsTrue((expectedUserMetadata == null && instrumentation.UserMetadata == null) || (expectedUserMetadata != null && instrumentation.UserMetadata != null && expectedUserMetadata.SequenceEqual(instrumentation.UserMetadata)));

            instrumentation.Reset();
        }

        /// <summary>
        /// The test server instrumentation class
        /// </summary>
        /// <seealso cref="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.IRingMasterServerInstrumentation" />
        private class TestServerInstrumentation : IRingMasterServerInstrumentation
        {
            /// <summary>
            /// Prevents a default instance of the <see cref="TestServerInstrumentation"/> class from being created.
            /// </summary>
            private TestServerInstrumentation()
            {
            }

            /// <summary>
            /// Gets or sets the singleton instance for the instrumentation
            /// </summary>
            public static IRingMasterServerInstrumentation Instance { get; set; } = new TestServerInstrumentation();

            public string Path { get; private set; }

            public byte[] Data { get; private set; }

            public byte[] UserMetadata { get; private set; }

            public IStat Stat { get; private set; }

            public int CalledCount { get; private set; }

            public void Reset()
            {
                this.Path = null;
                this.Data = null;
                this.UserMetadata = null;
                this.Stat = null;
            }

            public void CallbackBeforeComplete(string path, byte[] data, IStat stat, byte[] userMetadata)
            {
                this.Path = path;
                this.Data = data;
                this.UserMetadata = userMetadata;
                this.Stat = stat;
                this.CalledCount++;
            }

            public void OnAcquireLock(bool readOnly, bool succeeded, int level, TimeSpan elapsed)
            {
            }

            public void OnApply(long txtime, long xid)
            {
            }

            public void OnAuthFailed(InvalidAclException exception)
            {
            }

            public void OnBadRequest(ulong sid)
            {
            }

            public void OnBeforeLoadState()
            {
            }

            public void OnBeforeSaveState()
            {
            }

            public void OnConnectionRefused(string client)
            {
            }

            public void OnIncorrectExternalTransactionId(ulong externalTransactionId, ulong expectedTransactionId)
            {
            }

            public void OnLoadState(long totalMilliseconds)
            {
            }

            public void OnLockDownAccess(string nodepath, bool wasRwMode)
            {
            }

            public void OnLockDownFound(IEnumerable<string> nodepaths, bool replicationLockedDown)
            {
            }

            public void OnLostChild(string name)
            {
            }

            public void OnLostParent(string name)
            {
            }

            public void OnMeasurement(long measurement, string path, long xid)
            {
            }

            public void OnMeasurementCompleted(long milliseconds, long txId)
            {
            }

            public void OnNewRequest(ulong sid, string reqType, ulong callid)
            {
            }

            public void OnNewSession(ulong sid, string clientIdentity, string clientAddress)
            {
            }

            public void OnOrphanFound(string name)
            {
            }

            public void OnReplicaFinishedCheckpoint(TimeSpan elapsed)
            {
            }

            public void OnRequestCompleted(ulong sid, string reqType, ulong callid, string ok, double responsetimeInMillis)
            {
            }

            public void OnRequestDequeued(ulong sid, string reqType, ulong callid)
            {
            }

            public void OnRequestEnqueued(ulong sid, string reqType, ulong callid)
            {
            }

            public void OnResponseWaitForReplication(TimeSpan elapsed)
            {
            }

            public void OnSaveState(long totalMilliseconds)
            {
            }

            public void OnScheduledCommandFinished(bool executionSucceeded, long elapsedMilliseconds)
            {
            }

            public void OnScheduledCommandQueueChange(long numberInQueued)
            {
            }

            public void OnSessionClosed(ulong sid, string clientIdentity, string clientAddress)
            {
            }

            public void OnSslValidation(string client, bool ok)
            {
            }

            public void OnTransactionManagerBatchApplied(ulong batchId, ulong maxTransactionId)
            {
            }

            public void OnTxCommitted()
            {
            }

            public void OnUnexpectedException(string component, Exception ex)
            {
            }

            public void OnUpdateStatus(string version, TimeSpan uptime, bool isPrimary, int activeSessions)
            {
            }

            public void ReplicaHasCheckpointCoordinationEnabled()
            {
            }

            public void ReplicaHasCheckpointLease()
            {
            }

            public void ReplicaIsTakingCheckpoint()
            {
            }

            public void UpdateBulkWatcherCount(int globalWatchersCount)
            {
            }

            public void UpdateBulkWatcherExecutionBacklog(int pendingCount)
            {
            }

            public void UpdateEphemeralNodeCounts(ulong totalDataSize, ulong totalCount)
            {
            }

            public void UpdatePersistentNodeCounts(ulong totalDataSize, ulong totalCount)
            {
            }

            public void OnCompleteTerminationFailure()
            {
            }
        }
    }
}
