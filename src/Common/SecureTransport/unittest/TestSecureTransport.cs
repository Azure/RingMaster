// <copyright file="TestSecureTransport.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.SecureTransportUnitTest
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit tests for the SecureTransport class.
    /// </summary>
    [TestClass]
    public class TestSecureTransport
    {
        private int serverListenPort;
        private IPEndPoint[] clientEndPoints;

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.serverListenPort = GetAvailablePort(9000);
            string connectionString = string.Format("127.0.0.1:{0}", this.serverListenPort);
            this.clientEndPoints = SecureTransport.ParseConnectionString(connectionString);
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestParseConnectionStringSingleEndpoint()
        {
            IPEndPoint[] endpoints = SecureTransport.ParseConnectionString("127.0.0.1:99");
            Assert.AreEqual(1, endpoints.Length);
            Assert.AreEqual("127.0.0.1", endpoints[0].Address.ToString());
            Assert.AreEqual(99, endpoints[0].Port);
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestParseConnectionStringSingleHostAddress()
        {
            IPEndPoint[] endpoints = SecureTransport.ParseConnectionString("localhost:99");
            Assert.AreEqual(1, endpoints.Length);
            Assert.IsTrue(endpoints[0].Address.Equals(IPAddress.Loopback) || endpoints[0].Address.Equals(IPAddress.IPv6Loopback));
            Assert.AreEqual(99, endpoints[0].Port);
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestParseConnectionStringMultipleEndpoints()
        {
            IPEndPoint[] endpoints = SecureTransport.ParseConnectionString("127.0.0.1:99,10.11.12.13:990,8.8.8.8:8");
            Assert.AreEqual(3, endpoints.Length);
            Assert.AreEqual("127.0.0.1", endpoints[0].Address.ToString());
            Assert.AreEqual(99, endpoints[0].Port);

            Assert.AreEqual("10.11.12.13", endpoints[1].Address.ToString());
            Assert.AreEqual(990, endpoints[1].Port);

            Assert.AreEqual("8.8.8.8", endpoints[2].Address.ToString());
            Assert.AreEqual(8, endpoints[2].Port);
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartServerAfterStartClient()
        {
            using (var transport = CreateTransport())
            {
                transport.StartClient(this.clientEndPoints);
                VerifySecureTransportException(
                    SecureTransportException.Code.AlreadyStarted,
                    () =>
                    {
                        transport.StartServer(this.serverListenPort);
                    },
                    "StartServer after StartClient throws SecureTransportException with code AlreadyStarted");
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartClientAfterStartServer()
        {
            using (var transport = CreateTransport())
            {
                transport.StartServer(this.serverListenPort);
                VerifySecureTransportException(
                    SecureTransportException.Code.AlreadyStarted,
                    () =>
                    {
                        transport.StartClient(this.clientEndPoints);
                    },
                    "StartClient after StartServer throws SecureTransportException with code AlreadyStarted");
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestSecureTransportStopWithoutStart()
        {
            using (var transport = CreateTransport())
            {
                VerifySecureTransportException(
                    SecureTransportException.Code.NotStarted,
                    () => transport.Stop(),
                    "Stop without start throws SecureTransportException with code NotStarted");
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartServerAndStop()
        {
            using (var transport = CreateTransport())
            {
                transport.StartServer(this.serverListenPort);
                transport.Stop();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartServerAndClose()
        {
            var transport = CreateTransport();
            try
            {
                transport.StartServer(this.serverListenPort);
                transport.Close();
                transport = null;
            }
            finally
            {
                transport?.Dispose();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartServerAndDispose()
        {
            using (var transport = CreateTransport())
            {
                transport.StartServer(this.serverListenPort);
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartServerAndCancel()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var transport = CreateTransport(cancellationTokenSource.Token))
            {
                Task task = transport.StartServer(this.serverListenPort);

                Thread.Sleep(1000);
                cancellationTokenSource.Cancel();
                Assert.IsTrue(task.Wait(10000));
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartClientAndCancel()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var clientTransport = CreateTransport(cancellationTokenSource.Token))
            {
                Task task = clientTransport.StartClient(this.clientEndPoints);
                cancellationTokenSource.Cancel();
                Assert.IsTrue(task.Wait(25000));
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartClientAndStop()
        {
            using (var transport = CreateTransport())
            {
                transport.StartClient(this.clientEndPoints);
                transport.Stop();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartClientAndClose()
        {
            var transport = CreateTransport();
            try
            {
                transport.StartClient(this.clientEndPoints);
                transport.Close();
                transport = null;
            }
            finally
            {
                transport?.Dispose();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartClientAndDispose()
        {
            using (var transport = CreateTransport())
            {
                transport.StartClient(this.clientEndPoints);
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestRestartServer()
        {
            using (var transport = CreateTransport())
            {
                transport.StartServer(this.serverListenPort);
                transport.Stop();
                transport.StartServer(this.serverListenPort);
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestRestartClient()
        {
            using (var transport = CreateTransport())
            {
                transport.StartClient(this.clientEndPoints);
                transport.Stop();
                transport.StartClient(this.clientEndPoints);
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestSingleServerConnection()
        {
            var connectionEstablished = new ManualResetEventSlim();
            var connectionLost = new ManualResetEventSlim();

            SecureTransport serverTransport = null;
            SecureTransport clientTransport = null;

            try
            {
                serverTransport = CreateTransport();
                serverTransport.StartServer(this.serverListenPort);
                serverTransport.OnNewConnection = _ => connectionEstablished.Set();
                serverTransport.OnConnectionLost = () => connectionLost.Set();

                clientTransport = CreateTransport();
                clientTransport.StartClient(this.clientEndPoints);
                connectionEstablished.Wait();
            }
            finally
            {
                try
                {
                    serverTransport.Dispose();
                    Assert.IsTrue(connectionLost.Wait(5000));
                }
                finally
                {
                    clientTransport?.Dispose();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestMultipleServerConnections()
        {
            var connection1Established = new ManualResetEventSlim();
            var connection1Lost = new ManualResetEventSlim();
            var connection2Established = new ManualResetEventSlim();
            var connection2Lost = new ManualResetEventSlim();

            var port = GetAvailablePort(10000);
            var clientEndpoint = SecureTransport.ParseConnectionString($"127.0.0.1:{port}");

            SecureTransport serverTransport = null;
            SecureTransport clientTransport1 = null;
            SecureTransport clientTransport2 = null;

            try
            {
                serverTransport = CreateTransport();
                serverTransport.StartServer(port);

                clientTransport1 = CreateTransport();

                serverTransport.OnNewConnection = _ => connection1Established.Set();
                clientTransport1.OnConnectionLost = () => connection1Lost.Set();
                clientTransport1.StartClient(clientEndpoint);
                connection1Established.Wait();

                clientTransport2 = CreateTransport();

                serverTransport.OnNewConnection = _ => connection2Established.Set();
                clientTransport2.OnConnectionLost = () => connection2Lost.Set();
                clientTransport2.StartClient(clientEndpoint);
                connection2Established.Wait();
            }
            finally
            {
                try
                {
                    serverTransport.Dispose();
                    Assert.IsTrue(connection1Lost.Wait(5000));
                    Assert.IsTrue(connection2Lost.Wait(5000));
                }
                finally
                {
                    clientTransport1?.Dispose();
                    clientTransport2?.Dispose();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestClientCancellationToken()
        {
            using (var serverTransport = CreateTransport())
            {
                serverTransport.StartServer(this.serverListenPort);

                var connectionEstablished = new ManualResetEventSlim();
                var connectionLost = new ManualResetEventSlim();

                using (var cancellationTokenSource = new CancellationTokenSource())
                using (var clientTransport = CreateTransport(cancellationTokenSource.Token))
                {
                    clientTransport.OnNewConnection = connection =>
                    {
                        connection.OnConnectionLost = () =>
                        {
                            Trace.TraceInformation("Connection lost");
                            connectionLost.Set();
                        };

                        Trace.TraceInformation("Connection established");
                        connectionEstablished.Set();
                    };

                    Task clientTask = clientTransport.StartClient(this.clientEndPoints);
                    connectionEstablished.Wait();

                    Trace.TraceInformation("Cancelling");
                    cancellationTokenSource.Cancel();
                    connectionLost.Wait();

                    clientTask.Wait();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestServerCancellationToken()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var serverTransport = CreateTransport(cancellationTokenSource.Token))
            {
                Task serverTask = serverTransport.StartServer(this.serverListenPort);

                var connectionEstablished = new ManualResetEventSlim();
                var connectionLost = new ManualResetEventSlim();

                using (var clientTransport = CreateTransport(cancellationTokenSource.Token))
                {
                    clientTransport.OnNewConnection = connection =>
                    {
                        connection.OnConnectionLost = () =>
                        {
                            Trace.TraceInformation("Connection lost");
                            connectionLost.Set();
                        };

                        Trace.TraceInformation("Connection established");
                        connectionEstablished.Set();
                    };

                    clientTransport.StartClient(this.clientEndPoints);
                    connectionEstablished.Wait();

                    Trace.TraceInformation("Cancelling");
                    cancellationTokenSource.Cancel();
                    connectionLost.Wait();
                }

                serverTask.Wait();
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestMaxConnectionLifeSpan()
        {
            var connection1Established = new ManualResetEventSlim();
            var connection1Lost = new ManualResetEventSlim();

            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var serverTransport = CreateTransport(cancellationTokenSource.Token))
            {
                serverTransport.MaxConnectionLifeSpan = TimeSpan.FromSeconds(5);
                serverTransport.StartServer(this.serverListenPort);

                using (var clientTransport1 = CreateTransport())
                {
                    serverTransport.OnNewConnection = _ => connection1Established.Set();
                    clientTransport1.OnConnectionLost = () => connection1Lost.Set();
                    clientTransport1.StartClient(this.clientEndPoints);
                    connection1Established.Wait();

                    // The connections should be terminated after 5 seconds
                    Trace.TraceInformation("Waiting for connection to be lost");
                    connection1Lost.Wait();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestMaxConnectionIdleTime()
        {
            var connection1Established = new ManualResetEventSlim();
            var connection1Lost = new ManualResetEventSlim();

            var configuration = new SecureTransport.Configuration
            {
                MaxConnectionIdleTime = TimeSpan.FromSeconds(5),
                CommunicationProtocolVersion = 1,
            };

            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var serverTransport = new SecureTransport(configuration, null, cancellationTokenSource.Token))
            {
                serverTransport.StartServer(this.serverListenPort);

                using (var clientTransport1 = CreateTransport())
                {
                    serverTransport.OnNewConnection = _ => connection1Established.Set();
                    clientTransport1.OnConnectionLost = () => connection1Lost.Set();
                    clientTransport1.StartClient(this.clientEndPoints);
                    connection1Established.Wait();

                    // The connections should be terminated after 5 seconds
                    Trace.TraceInformation("Waiting for connection to be lost");
                    connection1Lost.Wait();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestDisconnectAfterDispose()
        {
            var connection1Established = new ManualResetEventSlim();
            var connection1Lost = new ManualResetEventSlim();

            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var serverTransport = CreateTransport(cancellationTokenSource.Token))
            {
                serverTransport.MaxConnectionLifeSpan = TimeSpan.FromSeconds(2);
                serverTransport.StartServer(this.serverListenPort);

                using (var clientTransport1 = CreateTransport())
                {
                    Communication.IConnection connection = null;
                    serverTransport.OnNewConnection = c =>
                    {
                        connection = c;
                        connection1Established.Set();
                    };

                    clientTransport1.OnConnectionLost = () => connection1Lost.Set();
                    clientTransport1.StartClient(this.clientEndPoints);
                    connection1Established.Wait();
                    Assert.IsNotNull(connection);
                    connection.Dispose();

                    // The connection lifetime timer may end up calling disconnect just after
                    // the connection has been disposed.
                    connection.Disconnect();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestSingleClientConnection()
        {
            var connectionEstablished = new ManualResetEventSlim();
            var connectionLost = new ManualResetEventSlim();

            SecureTransport serverTransport = null;
            SecureTransport clientTransport = null;

            try
            {
                serverTransport = CreateTransport();
                serverTransport.StartServer(this.serverListenPort);

                clientTransport = CreateTransport();
                clientTransport.OnNewConnection = _ => connectionEstablished.Set();
                clientTransport.OnConnectionLost = () => connectionLost.Set();

                clientTransport.StartClient(this.clientEndPoints);
                connectionEstablished.Wait();
            }
            finally
            {
                try
                {
                    serverTransport.Dispose();
                    Assert.IsTrue(connectionLost.Wait(5000));
                }
                finally
                {
                    clientTransport?.Dispose();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestClientConnectionRetrySameServer()
        {
            var connectionEstablished = new ManualResetEventSlim();
            var connectionLost = new ManualResetEventSlim();

            SecureTransport serverTransport = null;
            SecureTransport clientTransport = null;

            try
            {
                serverTransport = CreateTransport();
                serverTransport.StartServer(this.serverListenPort);

                clientTransport = CreateTransport();
                clientTransport.OnNewConnection = _ => connectionEstablished.Set();
                clientTransport.OnConnectionLost = () => connectionLost.Set();

                clientTransport.StartClient(this.clientEndPoints);
                connectionEstablished.Wait();

                // Now stop the server to break the established connection
                serverTransport.Stop();
                connectionLost.Wait();

                connectionEstablished.Reset();
                connectionLost.Reset();

                // Restart the server and verify that the client automatically connects to it
                serverTransport.StartServer(this.serverListenPort);
                connectionEstablished.Wait();
            }
            finally
            {
                try
                {
                    serverTransport.Dispose();
                    Assert.IsTrue(connectionLost.Wait(5000));
                }
                finally
                {
                    clientTransport?.Dispose();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestClientConnectionRetryDifferentServer()
        {
            var connectionEstablished = new ManualResetEventSlim();
            var connectionLost = new ManualResetEventSlim();

            int server1ListenPort = GetAvailablePort(9997);
            int server2ListenPort = GetAvailablePort(server1ListenPort + 1);
            IPEndPoint[] clientEndPoints = SecureTransport.ParseConnectionString(string.Format("127.0.0.1:{0},127.0.0.1:{1}", server1ListenPort, server2ListenPort));

            SecureTransport serverTransport1 = null;
            SecureTransport clientTransport = null;
            try
            {
                serverTransport1 = CreateTransport();
                serverTransport1.StartServer(server1ListenPort);

                clientTransport = CreateTransport();
                clientTransport.OnNewConnection = _ => connectionEstablished.Set();
                clientTransport.OnConnectionLost = () => connectionLost.Set();

                clientTransport.StartClient(clientEndPoints);
                connectionEstablished.Wait();

                // Now stop the server to break the established connection
                serverTransport1.Stop();
                connectionLost.Wait();

                connectionEstablished.Reset();
                connectionLost.Reset();

                using (var serverTransport2 = CreateTransport())
                {
                    // Start a new server at a different port and verify that the client automatically connects to it
                    serverTransport2.StartServer(server2ListenPort);
                    connectionEstablished.Wait();
                }
            }
            finally
            {
                try
                {
                    serverTransport1.Dispose();
                    Assert.IsTrue(connectionLost.Wait(5000));
                }
                finally
                {
                    clientTransport?.Dispose();
                }
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestStartServerWithDynamicPortAndStop()
        {
            using (var transport = CreateTransport())
            {
                transport.StartServer(0);

                // Wait until the server is active
                bool transportIsActive = false;
                for (int i = 0; i < 10 && !transportIsActive; i++)
                {
                    // IsAction means the server is *about* to start
                    if (transport.IsActive && transport.LocalEndpoint != null)
                    {
                        transportIsActive = true;
                    }
                    else
                    {
                        Thread.Sleep(125);
                    }
                }

                Assert.IsTrue(transportIsActive, "Transport should be active");

                bool portIsGood = false;
                for (int i = 0; i < 125 && !portIsGood; i++)
                {
                    if (((IPEndPoint)transport.LocalEndpoint).Port > 0)
                    {
                        portIsGood = true;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }

                Assert.IsTrue(portIsGood, "Local endpoint should have a valid port");
                transport.Stop();
            }
        }

        internal static int GetAvailablePort(int startPort)
        {
            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list to find a port that is not used.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            int[] usedPorts = tcpConnInfoArray.Select(connectionInformation => connectionInformation.LocalEndPoint.Port).ToArray();

            for (int i = startPort; i < 65536; i++)
            {
                if (!usedPorts.Contains(i))
                {
                    return i;
                }
            }

            Assert.Fail("Failed to find an available port");
            return 0;
        }

        /// <summary>
        /// Verify that the given action throws SecureTransportException with the correct code
        /// </summary>
        /// <param name "expectedCode">Expected SecureTransportException error code</param>
        /// <param name="action">Action that is expected to throw the exception</param>
        /// <param name="message">Error message to show if the task did not throw the exception</param>
        private static void VerifySecureTransportException(SecureTransportException.Code expectedCode, Action action, string message)
        {
            try
            {
                action();
            }
            catch (SecureTransportException ex)
            {
                Assert.AreEqual(expectedCode, ex.ErrorCode);
                return;
            }

            Assert.Fail(message);
        }

        private static SecureTransport CreateTransport()
        {
            return CreateTransport(CancellationToken.None);
        }

        private static SecureTransport CreateTransport(CancellationToken cancellationToken)
        {
            var configuration = new SecureTransport.Configuration
            {
                UseSecureConnection = false,
                CommunicationProtocolVersion = 1
            };

            return new SecureTransport(configuration, null, cancellationToken);
        }
    }
}
