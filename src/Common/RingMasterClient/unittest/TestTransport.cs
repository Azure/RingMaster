// <copyright file="TestTransport.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterClientUnitTest
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify that the ring master client responds correctly to transport notifications.
    /// </summary>
    [TestClass]
    public sealed class TestTransport : RingMasterClientUnitTest
    {
        /// <summary>
        /// Verifies that pending requests are completed with <c>Operationtimeout</c> error if the
        /// transport breaks the connection and no new connection is established before timeout.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestTransportConnectionLoss()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Assert.IsNotNull(transport.OnNewConnection);

                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                Assert.AreEqual(0, transport.Requests.Count);

                Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                Task existsTask = ringMaster.Exists("/test", watch: false);
                Task getChildrenTask = ringMaster.GetChildren("/", watch: false);
                Task getDataTask = ringMaster.GetData("/test", watch: false);
                Task getAclTask = ringMaster.GetACL("/test", new Stat());
                Task setDataTask = ringMaster.SetData("/test", null, -1);
                Task setAclTask = ringMaster.SetACL("/test", new List<Acl>(), -1);
                Task syncTask = ringMaster.Sync("/");
                Task multiTask = ringMaster.Multi(new List<Op>());
                Task deleteTask = ringMaster.Delete("/test", -1);

                // Wait for all 10 requests to be sent
                while (transport.Requests.Count < 10)
                {
                    Thread.Sleep(100);
                }

                Trace.TraceInformation("Breaking connection");
                transport.BreakConnection();

                Trace.TraceInformation("Waiting for pending requests");
                try
                {
                    Task.WaitAll(
                        createTask,
                        existsTask,
                        getChildrenTask,
                        getDataTask,
                        getAclTask,
                        setDataTask,
                        setAclTask,
                        syncTask,
                        multiTask,
                        deleteTask);

                    Assert.Fail("Tasks should have thrown RingMasterException with Operationtimeout");
                }
                catch (AggregateException)
                {
                    this.VerifyErrorResult(createTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(existsTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(getChildrenTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(getDataTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(getAclTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(setDataTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(setAclTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(syncTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(multiTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(deleteTask, RingMasterException.Code.Operationtimeout);
                }

                Assert.AreEqual(1, instrumentation.ConnectionCreatedCount);
                Assert.AreEqual(1, instrumentation.ConnectionClosedCount);
            }
        }

        /// <summary>
        /// Verifies that pending requests are completed with <c>ConnectionLoss</c> if the ringmaster
        /// client is closed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestClientClose()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            {
                RingMasterClient ringMaster = null;
                try
                {
                    ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None);
                    Assert.IsNotNull(transport.OnNewConnection);

                    Trace.TraceInformation("Establishing connection");
                    transport.EstablishConnection();

                    Assert.AreEqual(0, transport.Requests.Count);

                    Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                    Task existsTask = ringMaster.Exists("/test", watch: false);
                    Task getChildrenTask = ringMaster.GetChildren("/", watch: false);
                    Task getDataTask = ringMaster.GetData("/test", watch: false);
                    Task getAclTask = ringMaster.GetACL("/test", new Stat());
                    Task setDataTask = ringMaster.SetData("/test", null, -1);
                    Task setAclTask = ringMaster.SetACL("/test", new List<Acl>(), -1);
                    Task syncTask = ringMaster.Sync("/");
                    Task multiTask = ringMaster.Multi(new List<Op>());
                    Task deleteTask = ringMaster.Delete("/test", -1);

                    var sw = Stopwatch.StartNew();
                    // Wait for all 10 requests to be sent
                    while (transport.Requests.Count < 10)
                    {
                        Thread.Yield();
                    }

                    var expectedErrorCodes = sw.ElapsedMilliseconds > 1000 * 10
                        ? new[] { RingMasterException.Code.Operationtimeout, RingMasterException.Code.Connectionloss, }
                        : new[] { RingMasterException.Code.Connectionloss, };

                    Trace.TraceInformation("Closing ringmaster");
                    ringMaster.Close();
                    ringMaster = null;

                    Trace.TraceInformation("Waiting for pending requests");
                    try
                    {
                        Task.WaitAll(
                            createTask,
                            existsTask,
                            getChildrenTask,
                            getDataTask,
                            getAclTask,
                            setDataTask,
                            setAclTask,
                            syncTask,
                            multiTask,
                            deleteTask);

                        Assert.Fail("Tasks should have thrown RingMasterException with ConnectionLoss");
                    }
                    catch (AggregateException)
                    {
                        this.VerifyErrorResult(createTask, expectedErrorCodes);
                        this.VerifyErrorResult(existsTask, expectedErrorCodes);
                        this.VerifyErrorResult(getChildrenTask, expectedErrorCodes);
                        this.VerifyErrorResult(getDataTask, expectedErrorCodes);
                        this.VerifyErrorResult(getAclTask, expectedErrorCodes);
                        this.VerifyErrorResult(setDataTask, expectedErrorCodes);
                        this.VerifyErrorResult(setAclTask, expectedErrorCodes);
                        this.VerifyErrorResult(syncTask, expectedErrorCodes);
                        this.VerifyErrorResult(multiTask, expectedErrorCodes);
                        this.VerifyErrorResult(deleteTask, expectedErrorCodes);
                    }

                    Assert.AreEqual(1, instrumentation.ConnectionCreatedCount);
                    Assert.AreEqual(1, instrumentation.ConnectionClosedCount);
                }
                finally
                {
                    ringMaster?.Dispose();
                }
            }
        }

        /// <summary>
        /// Verifies that if the transport re-establishes a new connection after the previous connection
        /// is broken, subsequent requests are accepted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestConnectionRecovery()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(10000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Assert.IsNotNull(transport.OnNewConnection);

                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                Assert.AreEqual(0, transport.Requests.Count);

                Task beforeBreakTask = ringMaster.Create("/test1", null, null, CreateMode.Persistent);

                // Wait for the request to be sent
                while (transport.Requests.Count < 1)
                {
                    Thread.Sleep(100);
                }

                // Break and re-establish a new connection
                transport.BreakConnection();

                transport.EstablishConnection();

                Task afterBreakTask = ringMaster.Create("/test2", null, null, CreateMode.Persistent);

                // Wait for the request to be sent
                while (transport.Requests.Count < 2)
                {
                    Thread.Sleep(100);
                }

                // Verify that both tasks are still pending
                Assert.AreEqual(-1, Task.WaitAny(new Task[] { beforeBreakTask, afterBreakTask }, 1000));

                Assert.AreEqual(2, instrumentation.ConnectionCreatedCount);
                Assert.AreEqual(1, instrumentation.ConnectionClosedCount);
            }
        }

        /// <summary>
        /// Verifies that if no requests are sent then the client automatically sends heartbeats.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestHeartBeat()
        {
            var configuration = new RingMasterClient.Configuration() { HeartBeatInterval = TimeSpan.FromMilliseconds(100) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                Assert.AreEqual(0, transport.Requests.Count);

                // Wait for 5 heartbeats.
                while (instrumentation.LastHeartBeatId < 5)
                {
                    Thread.Sleep(100);
                }

                Assert.AreEqual(1, instrumentation.ConnectionCreatedCount);
                Assert.AreEqual(0, instrumentation.ConnectionClosedCount);
            }
        }

        /// <summary>
        /// Verifies that if a heartbeat request fails, the connection is disconnected.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestHeartBeatFailure()
        {
            var configuration = new RingMasterClient.Configuration() { HeartBeatInterval = TimeSpan.FromMilliseconds(100) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                transport.MustFailSend = true;

                Assert.AreEqual(0, transport.Requests.Count);

                // Wait until connection is closed because of heartbeat failure.
                while (instrumentation.ConnectionClosedCount < 1)
                {
                    Thread.Sleep(100);
                }

                Assert.AreEqual(1, instrumentation.ConnectionCreatedCount);
                Assert.AreEqual(1ul, instrumentation.LastHeartBeatId);
            }
        }

        /// <summary>
        /// Verifies that if there is no connection available, requests are timed out
        /// with the <c>Operationtimeout</c> error.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSendTimeout()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(100) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Assert.IsNotNull(transport.OnNewConnection);
                Task task = ringMaster.Create("/test", null, null, CreateMode.Persistent);

                try
                {
                    task.Wait();
                }
                catch (AggregateException)
                {
                    this.VerifyErrorResult(task, expectedCode: RingMasterException.Code.Operationtimeout);
                }

                Assert.AreEqual(1, instrumentation.RequestQueuedCount);
                Assert.AreEqual(0, instrumentation.RequestSentCount);
                Assert.AreEqual(1, instrumentation.ResponseReceivedCount);
                Assert.AreEqual(1, instrumentation.RequestTimedOutCount);
            }
        }

        /// <summary>
        /// Verifies that requests are failed with ReqeustQueueFull exception if the request queue is full.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestRequestQueueFull()
        {
            var configuration = new RingMasterClient.Configuration();
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            configuration.DefaultTimeout = TimeSpan.FromMilliseconds(1000);
            configuration.RequestQueueLength = 10;

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Assert.IsNotNull(transport.OnNewConnection);

                // Fill the queue with configuration.RequestQueueLength requests.
                var tasks = new List<Task>();
                for (int i = 0; i < configuration.RequestQueueLength; i++)
                {
                    tasks.Add(ringMaster.Create("/test", null, null, CreateMode.Persistent));
                }

                // Now attempt to add one more request.
                Task taskAfterQueueFull = null;
                try
                {
                    taskAfterQueueFull = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                    taskAfterQueueFull.Wait();
                    Assert.Fail("Attempting to add a Request after queue is full must result in RingMasterClientException");
                }
                catch (AggregateException)
                {
                    Assert.IsTrue(taskAfterQueueFull.Exception.InnerException is RingMasterClientException);
                }

                try
                {
                    Task.WaitAll(tasks.ToArray());
                }
                catch (AggregateException)
                {
                    foreach (var task in tasks)
                    {
                        this.VerifyErrorResult(task, expectedCode: RingMasterException.Code.Operationtimeout);
                    }
                }

                Assert.AreEqual(10, instrumentation.RequestQueuedCount);
                Assert.AreEqual(1, instrumentation.RequestQueueFullCount);
                Assert.AreEqual(0, instrumentation.RequestSentCount);
                Assert.AreEqual(10, instrumentation.ResponseReceivedCount);
                Assert.AreEqual(10, instrumentation.RequestTimedOutCount);
            }
        }

        /// <summary>
        /// Verifies that pending requests are drained with Connectionloss error if the client is closed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDrainRequests()
        {
            var configuration = new RingMasterClient.Configuration();
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();
            using (var transport = new DummyTransport(protocol))
            {
                configuration.DefaultTimeout = TimeSpan.FromMilliseconds(1000);
                configuration.RequestQueueLength = 10;

                using (var cancellationTokenSource = new CancellationTokenSource())
                using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, cancellationTokenSource.Token))
                {
                    Assert.IsNotNull(transport.OnNewConnection);

                    // Fill the queue with configuration.RequestQueueLength requests.
                    var tasks = new List<Task>();
                    for (int i = 0; i < configuration.RequestQueueLength; i++)
                    {
                        tasks.Add(ringMaster.Create("/test", null, null, CreateMode.Persistent));
                    }

                    // Now cancel the token to drain the requests.
                    cancellationTokenSource.Cancel();

                    try
                    {
                        Task.WaitAll(tasks.ToArray());
                    }
                    catch (AggregateException)
                    {
                        foreach (var task in tasks)
                        {
                            this.VerifyErrorResult(task, expectedCode: RingMasterException.Code.Connectionloss);
                        }
                    }

                    Assert.AreEqual(10, instrumentation.RequestQueuedCount);
                    Assert.AreEqual(0, instrumentation.RequestSentCount);
                    Assert.AreEqual(10, instrumentation.ResponseReceivedCount);
                    Assert.AreEqual(0, instrumentation.RequestTimedOutCount);
                    Assert.AreEqual(10, instrumentation.RequestAbortedCount);
                }
            }
        }

        /// <summary>
        /// Verifies that if a request send fails, the request times out.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSendFailure()
        {
            var configuration = new RingMasterClient.Configuration();
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();
            using (var transport = new DummyTransport(protocol))
            {
                configuration.DefaultTimeout = TimeSpan.FromMilliseconds(1000);

                using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
                {
                    const RingMasterException.Code ExpectedTimeoutCode = RingMasterException.Code.Operationtimeout;
                    Assert.IsNotNull(transport.OnNewConnection);

                    Trace.TraceInformation("Establishing connection");
                    transport.EstablishConnection();

                    Assert.AreEqual(0, transport.Requests.Count);

                    transport.MustFailSend = true;

                    Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                    Task existsTask = ringMaster.Exists("/test", false);
                    Task getChildrenTask = ringMaster.GetChildren("/", false);

                    Trace.TraceInformation("Waiting for pending requests");
                    try
                    {
                        Task.WaitAll(
                            createTask,
                            existsTask,
                            getChildrenTask);

                        Assert.Fail("Tasks should have thrown RingMasterException with Operationtimeout");
                    }
                    catch (AggregateException)
                    {
                        this.VerifyErrorResult(createTask, ExpectedTimeoutCode);
                        this.VerifyErrorResult(existsTask, ExpectedTimeoutCode);
                        this.VerifyErrorResult(getChildrenTask, ExpectedTimeoutCode);
                    }

                    Assert.AreEqual(1, instrumentation.RequestSentCount);
                    Assert.AreEqual(4, instrumentation.ResponseReceivedCount);

                    Assert.AreEqual(3, instrumentation.RequestQueuedCount);
                    Assert.AreEqual(0, instrumentation.RequestQueueFullCount);
                    Assert.AreEqual(3, instrumentation.RequestSendFailedCount);
                    Assert.AreEqual(3, instrumentation.RequestTimedOutCount);
                }
            }
        }

        /// <summary>
        /// Verifies that if a request does not receive a response within the timeout limit it gets timed out
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestResponseTimeout()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                const RingMasterException.Code ExpectedTimeoutCode = RingMasterException.Code.Operationtimeout;
                Assert.IsNotNull(transport.OnNewConnection);

                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                Assert.AreEqual(0, transport.Requests.Count);

                Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                Task existsTask = ringMaster.Exists("/test", false);
                Task getChildrenTask = ringMaster.GetChildren("/", false);

                // Wait for the request to be sent
                while (transport.Requests.Count < 3)
                {
                    Thread.Sleep(100);
                }

                Trace.TraceInformation("Waiting for pending requests");
                try
                {
                    Task.WaitAll(
                        createTask,
                        existsTask,
                        getChildrenTask);

                    Assert.Fail("Tasks should have thrown RingMasterException with Operationtimeout");
                }
                catch (AggregateException)
                {
                    this.VerifyErrorResult(createTask, ExpectedTimeoutCode);
                    this.VerifyErrorResult(existsTask, ExpectedTimeoutCode);
                    this.VerifyErrorResult(getChildrenTask, ExpectedTimeoutCode);
                }

                Assert.AreEqual(4, instrumentation.RequestSentCount);
                Assert.AreEqual(4, instrumentation.ResponseReceivedCount);
                Assert.AreEqual(3, instrumentation.RequestTimedOutCount);

                // Verify the timer was re-scheduled and the timeout functionality still works
                Task getDataTask = ringMaster.GetData("/test", false);

                // Wait for the request to be sent
                while (transport.Requests.Count < 4)
                {
                    Thread.Sleep(100);
                }

                // Make sure it doesn't timeout too soon
                Assert.IsFalse(getDataTask.IsFaulted);

                try
                {
                    Task.WaitAll(getDataTask);

                    Assert.Fail("Task should have thrown RingMasterException with Operationtimeout");
                }
                catch (AggregateException)
                {
                    this.VerifyErrorResult(getDataTask, ExpectedTimeoutCode);
                }

                Assert.AreEqual(5, instrumentation.RequestSentCount);
                Assert.AreEqual(5, instrumentation.ResponseReceivedCount);
                Assert.AreEqual(4, instrumentation.RequestTimedOutCount);
            }
        }

        /// <summary>
        /// Verifies that if <c>Init</c> fails for a request, the connection is broken.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestInitFailure()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();
            using (var transport = new DummyTransport(protocol))
            {
                transport.MustFailInit = true;
                using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
                {
                    Assert.IsNotNull(transport.OnNewConnection);

                    // Init will fail when the connection is established.
                    Trace.TraceInformation("Establishing connection");
                    transport.EstablishConnection();

                    // Subsequent requests must fail with operationtimeout
                    // because there is no connection
                    Assert.AreEqual(0, transport.Requests.Count);

                    Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                    Assert.AreEqual(0, transport.Requests.Count);

                    Task existsTask = ringMaster.Exists("/test", false);
                    Assert.AreEqual(0, transport.Requests.Count);

                    Task getChildrenTask = ringMaster.GetChildren("/", false);
                    Assert.AreEqual(0, transport.Requests.Count);

                    // All the requests must fail with Operationtimeout error.
                    Trace.TraceInformation("Waiting for pending requests");
                    try
                    {
                        Task.WaitAll(
                            createTask,
                            existsTask,
                            getChildrenTask);

                        Assert.Fail("Tasks should have thrown RingMasterException with Operationtimeout");
                    }
                    catch (AggregateException)
                    {
                        this.VerifyErrorResult(createTask, RingMasterException.Code.Operationtimeout);
                        this.VerifyErrorResult(existsTask, RingMasterException.Code.Operationtimeout);
                        this.VerifyErrorResult(getChildrenTask, RingMasterException.Code.Operationtimeout);
                    }

                    // The connection must have been disconnected due to init failure.
                    Assert.IsTrue(transport.IsDisconnected);

                    Assert.AreEqual(3, instrumentation.RequestQueuedCount);
                    Assert.AreEqual(3, instrumentation.RequestTimedOutCount);
                }
            }
        }

        /// <summary>
        /// Verifies that invalid response packet is handled properly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestInvalidResponse()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Assert.IsNotNull(transport.OnNewConnection);

                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                transport.MustSendInvalidResponse = true;

                Assert.AreEqual(0, transport.Requests.Count);

                Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                Task existsTask = ringMaster.Exists("/test", false);
                Task getChildrenTask = ringMaster.GetChildren("/", false);

                Trace.TraceInformation("Waiting for pending requests");

                try
                {
                    Task.WaitAll(
                        createTask,
                        existsTask,
                        getChildrenTask);

                    Assert.Fail("Tasks should have thrown RingMasterException with Connectionloss");
                }
                catch (AggregateException)
                {
                    this.VerifyErrorResult(createTask, RingMasterException.Code.Connectionloss);
                    this.VerifyErrorResult(existsTask, RingMasterException.Code.Connectionloss);
                    this.VerifyErrorResult(getChildrenTask, RingMasterException.Code.Connectionloss);
                }

                // Expect 4 responses to be received - Response for Init + 3 invalid responses for the 3 tasks
                // Note that ResponseQueuedCount may not be 4 -- once the first invalid response is processed,
                // RingMasterRequestHandler will be cancelled, pending request will not be sent, no more response
                // is received from the dummy transport.
                Assert.AreEqual(4, instrumentation.ResponseReceivedCount);

                // The connection must have terminated after processing the first invalid response
                Assert.AreEqual(1, instrumentation.InvalidPacketReceivedCount);
            }
        }

        /// <summary>
        /// Verifies that unexpected response is handled properly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestUnexpectedResponse()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Assert.IsNotNull(transport.OnNewConnection);

                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                transport.MustSendUnexpectedResponse = true;

                Assert.AreEqual(0, transport.Requests.Count);

                Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                Task existsTask = ringMaster.Exists("/test", false);
                Task getChildrenTask = ringMaster.GetChildren("/", false);

                Trace.TraceInformation("Waiting for pending requests");
                try
                {
                    Task.WaitAll(
                        createTask,
                        existsTask,
                        getChildrenTask);

                    Assert.Fail("Tasks should have thrown RingMasterException with Operationtimeout");
                }
                catch (AggregateException)
                {
                    this.VerifyErrorResult(createTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(existsTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(getChildrenTask, RingMasterException.Code.Operationtimeout);
                }

                Assert.AreEqual(3, instrumentation.RequestTimedOutCount);
                Assert.AreEqual(3, instrumentation.UnexpectedResponseCount);
            }
        }

        /// <summary>
        /// Verifies that invalid message to client is handled properly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestInvalidMessageToClient()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                Assert.IsNotNull(transport.OnNewConnection);

                Trace.TraceInformation("Establishing connection");
                transport.EstablishConnection();

                Assert.AreEqual(0, transport.Requests.Count);

                transport.MustSendInvalidClientMessage = true;

                Task createTask = ringMaster.Create("/test", null, null, CreateMode.Persistent);
                Task existsTask = ringMaster.Exists("/test", false);
                Task getChildrenTask = ringMaster.GetChildren("/", false);

                Trace.TraceInformation("Waiting for pending requests");
                try
                {
                    Task.WaitAll(
                        createTask,
                        existsTask,
                        getChildrenTask);

                    Assert.Fail("Tasks should have thrown RingMasterException with Operationtimeout");
                }
                catch (AggregateException)
                {
                    this.VerifyErrorResult(createTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(existsTask, RingMasterException.Code.Operationtimeout);
                    this.VerifyErrorResult(getChildrenTask, RingMasterException.Code.Operationtimeout);
                }

                Assert.AreEqual(3, instrumentation.RequestTimedOutCount);
                Assert.AreEqual(0, instrumentation.InvalidPacketReceivedCount);
                Assert.AreEqual(3, instrumentation.InvalidClientMessageReceivedCount);
            }
        }

        /// <summary>
        /// Verifies that watcher notification message sent to a watcher that does not exist is handled properly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestNotificationToNonExistentWatcher()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var watcher = new TestWatcher(100, expectedNotificationsCount: 1))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                transport.EstablishConnection();
                transport.MustSendSuccessResponse = true;

                ringMaster.GetData("/", watcher).GetAwaiter().GetResult();

                transport.SendNotificationToWatcherId(10);
                transport.SendNotificationToWatcherId(ulong.MaxValue);
                transport.SendNotificationToAllWatchers();

                watcher.WaitForExpectedNotifications();
            }

            // Both notifications must result in WatcherNotFoundCount being incremented
            Assert.AreEqual(0, instrumentation.InvalidPacketReceivedCount);
            Assert.AreEqual(0, instrumentation.InvalidClientMessageReceivedCount);
            Assert.AreEqual(2, instrumentation.WatcherNotFoundCount);
        }

        /// <summary>
        /// Verifies that watcher notification message sent to a one use watcher causes that watcher to be removed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestNotificationToOneUseWatcher()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            using (var multiUseWatcher = new TestWatcher(0, expectedNotificationsCount: 2))
            using (var singleUseWatcher = new TestWatcher(1, expectedNotificationsCount: 1))
            using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
            {
                transport.EstablishConnection();
                transport.MustSendSuccessResponse = true;

                ringMaster.GetData("/", singleUseWatcher).GetAwaiter().GetResult();
                ringMaster.GetData("/", multiUseWatcher).GetAwaiter().GetResult();

                Trace.TraceInformation("Sending first notification to all watchers");
                transport.SendNotificationToAllWatchers();

                Trace.TraceInformation("Sending second notification to all watchers");
                transport.SendNotificationToAllWatchers();

                Trace.TraceInformation("SingleUseWatcher: Waiting for expectedNotifications");
                singleUseWatcher.WaitForExpectedNotifications();

                Trace.TraceInformation("MultiUseWatcher: Waiting for expectedNotifications");
                multiUseWatcher.WaitForExpectedNotifications();
            }

            // The first notification should have been delivered to the watcher - incrementing the WatcherNotificationCount
            // then, the watcher must have been removed - causing the second notification to increment the WatcherNotFoundCount
            Assert.AreEqual(4, instrumentation.WatcherNotificationCount);
            Assert.AreEqual(1, instrumentation.WatcherNotFoundCount);
        }

        /// <summary>
        /// Verifies that notification messages sent to a multi use watcher are handled properly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestNotificationsToMultiUseWatcher()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();
            const int NotificationCount = 10;

            using (var transport = new DummyTransport(protocol))
            using (var watcher = new TestWatcher(200, expectedNotificationsCount: NotificationCount))
            {
                using (var ringMaster = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None))
                {
                    transport.EstablishConnection();
                    transport.MustSendSuccessResponse = true;

                    ringMaster.GetData("/", watcher).GetAwaiter().GetResult();

                    for (int i = 0; i < NotificationCount; i++)
                    {
                        transport.SendNotificationToAllWatchers();
                    }

                    watcher.WaitForExpectedNotifications();
                }

                watcher.WaitForWatcherRemovedNotification();
            }

            // All notifications must have been delivered to the watcher.
            Assert.AreEqual(NotificationCount + 1, instrumentation.WatcherNotificationCount);
            Assert.AreEqual(0, instrumentation.WatcherNotFoundCount);
        }

        /// <summary>
        /// Verify that once cancellation is requested, further sends are rejected.
        /// </summary>
        /// <remarks>
        /// This is a repro case for: https://msazure.visualstudio.com/One/Networking-RingMaster/_workitems/edit/1584443
        /// </remarks>
        [TestMethod]
        [Timeout(30000)]
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "This test verifies a race condition that happens between close and dispose")]
        public void TestFailSendIfCancellationIsRequested()
        {
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(1000) };
            var instrumentation = new RingMasterClientInstrumentation();
            ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

            using (var transport = new DummyTransport(protocol))
            {
                RingMasterClient client = null;
                try
                {
                    Trace.TraceInformation("Creating RingMasterClient");
                    client = new RingMasterClient(configuration, instrumentation, protocol, transport, CancellationToken.None);

                    Trace.TraceInformation("Establishing connection");
                    transport.EstablishConnection();

                    Trace.TraceInformation("Sending heartbeat");
                    client.Exists("<Fail>", watcher: null, ignoreNonodeError: true).GetAwaiter().GetResult();

                    Trace.TraceInformation("Closing client");
                    client.Close();

                    Trace.TraceInformation("Configuring transport to drop any init requests");
                    transport.MustDropInit = true;
                    Trace.TraceInformation("Establishing connection again");
                    transport.EstablishConnection();

                    client.Dispose();
                    client = null;

                    Trace.TraceInformation("Client did not block waiting for init to complete");
                }
                finally
                {
                    if (client != null)
                    {
                        client.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Verify that the task faulted with a RingMasterException with <paramref name="expectedCode"/> error code.
        /// </summary>
        /// <param name="task">Task whose result must be verified</param>
        /// <param name="expectedCode">Expected error code</param>
        private void VerifyErrorResult(Task task, RingMasterException.Code expectedCode = RingMasterException.Code.Connectionloss) =>
            this.VerifyErrorResult(task, new[] { expectedCode });

        private void VerifyErrorResult(Task task, IEnumerable<RingMasterException.Code> expectedCodes)
        {
            Assert.IsTrue(task.IsFaulted, "Task should be faulted");
            Assert.IsTrue(task.Exception.InnerException is RingMasterException);
            var exception = (RingMasterException)task.Exception.InnerException;
            Assert.IsTrue(
                expectedCodes.Any(c => c == exception.ErrorCode),
                $"Actual exception is {exception.ErrorCode}, expected is: " + string.Join(", ", expectedCodes));
        }

        private sealed class TestWatcher : IWatcher, IDisposable
        {
            private readonly CountdownEvent expectedNotifications;
            private readonly ManualResetEvent watcherRemovedNotificationReceived;

            public TestWatcher(ulong id, int expectedNotificationsCount)
            {
                this.Id = id;
                this.Kind = expectedNotificationsCount == 1 ? WatcherKind.OneUse : default(WatcherKind);
                this.expectedNotifications = new CountdownEvent(expectedNotificationsCount);
                this.watcherRemovedNotificationReceived = new ManualResetEvent(false);
            }

            public ulong Id { get; private set; }

            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            public WatcherKind Kind { get; private set; }

            public void Process(WatchedEvent evt)
            {
                if (evt == null)
                {
                    throw new ArgumentNullException(nameof(evt));
                }

                if (!this.expectedNotifications.IsSet)
                {
                    this.expectedNotifications.Signal();
                }

                if (evt.EventType == WatchedEvent.WatchedEventType.WatcherRemoved)
                {
                    this.watcherRemovedNotificationReceived.Set();
                }
            }

            public void WaitForExpectedNotifications()
            {
                this.expectedNotifications.Wait();
            }

            public void WaitForWatcherRemovedNotification()
            {
                this.watcherRemovedNotificationReceived.WaitOne();
            }

            public void Dispose()
            {
                this.expectedNotifications.Dispose();
                this.watcherRemovedNotificationReceived.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is helper class")]
        private class DummyTransport : ITransport
        {
            private readonly ICommunicationProtocol protocol;
            private Connection activeConnection;
            private long nextConnectionId;

            public DummyTransport(ICommunicationProtocol protocol)
            {
                this.protocol = protocol;
                this.Requests = new List<RequestCall>();
                this.MustFailInit = false;
                this.MustFailSend = false;
                this.MustSendInvalidResponse = false;
                this.MustSendInvalidClientMessage = false;
                this.MustSendUnexpectedResponse = false;
                this.IsDisconnected = false;
            }

            public bool MustFailInit { get; set; }

            public bool MustDropInit { get; set; }

            public bool MustFailSend { get; set; }

            public bool MustSendSuccessResponse { get; set; }

            public bool MustSendInvalidResponse { get; set; }

            public bool MustSendInvalidClientMessage { get; set; }

            public bool MustSendUnexpectedResponse { get; set; }

            public bool IsDisconnected { get; set; }

            public Action OnConnectionLost { get; set; }

            public Action<IConnection> OnNewConnection { get; set; }

            public IList<RequestCall> Requests { get; private set; }

            /// <summary>
            /// Gets or sets teh callback that must be invoked for protocol negotiation
            /// </summary>
            public ProtocolNegotiatorDelegate OnProtocolNegotiation { get; set; }

            public bool UseNetworkByteOrder { get; set; } = false;

            public void EstablishConnection()
            {
                ulong id = (ulong)Interlocked.Increment(ref this.nextConnectionId);
                this.activeConnection = new Connection(id, this, this.protocol);
                this.IsDisconnected = false;
                this.OnNewConnection(this.activeConnection);
            }

            public void BreakConnection()
            {
                if (this.activeConnection != null)
                {
                    this.activeConnection.OnConnectionLost?.Invoke();

                    this.activeConnection = null;

                    this.OnConnectionLost?.Invoke();
                }
            }

            public void SendNotificationToAllWatchers()
            {
                this.activeConnection?.SendNotificationToAllWatchers();
            }

            public void SendNotificationToWatcherId(ulong watcherId)
            {
                this.activeConnection?.SendNotificationToWatcherId(watcherId);
            }

            public void Close()
            {
                this.BreakConnection();
            }

            public void Dispose()
            {
                this.Close();
                if (this.activeConnection != null)
                {
                    this.activeConnection.Dispose();
                }
            }

            private class Connection : IConnection
            {
                private readonly ICommunicationProtocol protocol;
                private readonly DummyTransport transport;
                private readonly ConcurrentDictionary<ulong, IWatcher> watchers = new ConcurrentDictionary<ulong, IWatcher>();

                public Connection(ulong id, DummyTransport transport, ICommunicationProtocol protocol)
                {
                    this.Id = id;
                    this.transport = transport;
                    this.protocol = protocol;
                }

                public Action OnConnectionLost { get; set; }

                public Action<byte[]> OnPacketReceived { get; set; }

                public ulong Id { get; private set; }

                public EndPoint RemoteEndPoint
                {
                    get
                    {
                        return new DummyEndPoint();
                    }
                }

                public string RemoteIdentity
                {
                    get
                    {
                        return $"[ConnectionId={this.Id}]";
                    }
                }

                public uint ProtocolVersion
                {
                    get
                    {
                        return RingMasterCommunicationProtocol.MaximumSupportedVersion;
                    }
                }

                public PacketReceiveDelegate DoPacketReceive
                {
                    get;
                    set;
                }

                public ProtocolNegotiatorDelegate DoProtocolNegotiation
                {
                    get;
                    set;
                }

                public void Dispose()
                {
                }

                public void Disconnect()
                {
                    this.transport.IsDisconnected = true;
                }

                public Task SendAsync(byte[] packet)
                {
                    if (packet == null)
                    {
                        throw new ArgumentNullException(nameof(packet));
                    }

                    RequestCall call = this.protocol.DeserializeRequest(packet, packet.Length, this.ProtocolVersion);
                    RequestResponse response = new RequestResponse();
                    response.CallId = call.CallId;
                    response.ResponsePath = call.Request.Path;
                    response.ResultCode = (int)RingMasterException.Code.Ok;

                    Trace.TraceInformation($"Send connection={this.Id} requestId={call.CallId}");
                    switch (call.Request.RequestType)
                    {
                        case RingMasterRequestType.Init:
                        {
                            if (this.transport.MustFailInit)
                            {
                                Trace.TraceInformation($"Failing InitRequest {call.CallId}");
                                response.ResultCode = (int)RingMasterException.Code.Operationtimeout;
                            }
                            else if (this.transport.MustDropInit)
                            {
                                Trace.TraceInformation($"Dropping InitRequest {call.CallId}");
                                return Task.FromResult<object>(null);
                            }
                            else
                            {
                                Trace.TraceInformation($"Processing InitRequest {call.CallId}");
                                RequestInit initRequest = (RequestInit)call.Request;
                                response.Content = new string[] { string.Empty + initRequest.SessionId, Guid.NewGuid().ToString() };
                            }

                            this.OnPacketReceived(this.protocol.SerializeResponse(response, this.ProtocolVersion));
                            return Task.FromResult<object>(null);
                        }

                        case RingMasterRequestType.GetData:
                            {
                                RequestGetData getDataRequest = (RequestGetData)call.Request;
                                if (getDataRequest.Watcher != null)
                                {
                                    this.watchers.TryAdd(getDataRequest.Watcher.Id, getDataRequest.Watcher);
                                }

                                break;
                            }

                        default:
                            break;
                    }

                    if (this.transport.MustFailSend)
                    {
                        Trace.TraceInformation($"DummyTransport.Connection FailingSend connectionId={this.Id}, requestId={call.CallId}");
                        throw new InvalidOperationException();
                    }
                    else if (this.transport.MustSendInvalidResponse)
                    {
                        Trace.TraceInformation($"DummyTransport.Connection SendingInvalidResponse connectionId={this.Id}, requestId={call.CallId}");
                        this.OnPacketReceived(Guid.NewGuid().ToByteArray());
                    }
                    else if (this.transport.MustSendInvalidClientMessage)
                    {
                        Trace.TraceInformation($"DummyTransport.Connection SendingInvalidClientMessage connectionId={this.Id}, requestId={call.CallId}");
                        RequestResponse clientMessage = new RequestResponse();
                        clientMessage.CallId = ulong.MaxValue;
                        this.OnPacketReceived(this.protocol.SerializeResponse(clientMessage, this.ProtocolVersion));
                    }
                    else if (this.transport.MustSendUnexpectedResponse)
                    {
                        Trace.TraceInformation($"DummyTransport.Connection SendingUnexpectedResponse connectionId={this.Id}, requestId={call.CallId}");
                        RequestResponse unexpectedResponse = new RequestResponse();
                        unexpectedResponse.CallId = call.CallId + 1000000;
                        this.OnPacketReceived(this.protocol.SerializeResponse(unexpectedResponse, this.ProtocolVersion));
                    }
                    else if ((call.Request.RequestType == RingMasterRequestType.Exists) && (call.Request.Path == "<Fail>"))
                    {
                        Trace.TraceInformation($"DummyTransport.Connection HeartBeat connectionId={this.Id}, requestId={call.CallId}");
                        response.ResultCode = (int)RingMasterException.Code.Nonode;
                        this.OnPacketReceived(this.protocol.SerializeResponse(response, this.ProtocolVersion));
                    }
                    else if (this.transport.MustSendSuccessResponse)
                    {
                        Trace.TraceInformation($"DummyTransport.Connection SendingSuccessResponse connectionId={this.Id}, requestId={call.CallId}");
                        Task.Run(() => this.OnPacketReceived(this.protocol.SerializeResponse(response, this.ProtocolVersion)));
                    }
                    else
                    {
                        Trace.TraceInformation($"DummyTransport.Connection Request connectionId={this.Id}, requestId={call.CallId}, Type={call.Request.RequestType}, path={call.Request.Path}");
                        this.transport.Requests.Add(call);
                    }

                    return Task.FromResult<object>(null);
                }

                public void Send(byte[] packet)
                {
                    this.SendAsync(packet).Wait();
                }

                public void SendNotificationToAllWatchers()
                {
                    foreach (var watcher in this.watchers)
                    {
                        this.SendNotificationToWatcherId(watcher.Key);
                    }
                }

                public void SendNotificationToWatcherId(ulong watcherId)
                {
                    RequestResponse clientMessage = new RequestResponse();
                    clientMessage.CallId = ulong.MaxValue;

                    clientMessage.Content = new WatcherCall()
                    {
                        WatcherId = watcherId,
                        Kind = default(WatcherKind),
                        WatcherEvt = new WatchedEvent(WatchedEvent.WatchedEventType.None, WatchedEvent.WatchedEventKeeperState.Unknown, string.Empty)
                    };

                    this.OnPacketReceived(this.protocol.SerializeResponse(clientMessage, this.ProtocolVersion));
                }

                private class DummyEndPoint : EndPoint
                {
                    public override string ToString() => "DummyEndPoint";
                }
            }
        }
    }
}
