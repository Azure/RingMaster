// <copyright file="SecureTransportTest.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.MiscellaneousTests
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for SecureTransport
    /// </summary>
    [TestClass]
    public class SecureTransportTest
    {
        // Client count to try connection.
        private const int ClientCount = 5;

        // Max allowed connection count.
        private const int MaxConnections = 3;

        // TCP port listened by server.
        private const int PortNumber = 34567;

        /// <summary>
        /// Logging delegate
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Initializes the test.
        /// </summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void TestClassInitialize(TestContext context)
        {
            if (context.GetType().Name.StartsWith("Dummy"))
            {
                log = s => context.WriteLine(s);
            }
            else
            {
                log = s => Trace.TraceInformation(s);
            }
        }

        /// <summary>
        /// Test connection failure after max connections are reached
        /// </summary>
        [TestMethod]
        public void TestOverLimitConnection()
        {
            // Create server task
            var tokenSource = new CancellationTokenSource();
            var serverTask = this.StartTransportServer(PortNumber, MaxConnections, tokenSource.Token);

            // Create multiple client tasks to connect to server.
            Task<bool>[] tasks = new Task<bool>[ClientCount];
            for (int i = 0; i < ClientCount; i++)
            {
                tasks[i] = this.StartTransportClient($"{i}", PortNumber);
            }

            // Wait for client task to finish or timeout. Count success results
            Task.WhenAll(tasks).Wait();

            int successCount = 0;
            int failureCount = 0;
            for (int i = 0; i < ClientCount; i++)
            {
                if (tasks[i].IsCompleted)
                {
                    bool result = tasks[i].GetAwaiter().GetResult();
                    if (result)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
            }

            tokenSource.Cancel();
            serverTask.Wait();

            Assert.AreEqual(MaxConnections, successCount);
            Assert.AreEqual(ClientCount - MaxConnections, failureCount);
        }

        /// <summary>
        /// Start a SecureTransport server.
        /// </summary>
        /// <param name="port">The server port to connect to.</param>
        /// <param name="maxConnections">Max clients to allow.</param>
        /// <param name="cancellationToken">Cancellation token to stop the server.</param>
        private Task StartTransportServer(int port, int maxConnections, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var configuration = new SecureTransport.Configuration()
                {
                    CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                    MaxConnections = maxConnections,
                };

                using (var transport = new SecureTransport(configuration))
                {
                    transport.OnNewConnection = connection =>
                    {
                        log($"Connection Established with {connection.RemoteEndPoint}");

                        connection.OnPacketReceived = packet =>
                        {
                            connection.SendAsync(packet);
                            return Task.CompletedTask;
                        };

                        connection.OnConnectionLost = () =>
                        {
                            log($"Connection with {connection.RemoteEndPoint} was lost");
                        };
                    };

                    Task serverTask = transport.StartServer(port);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Task.Yield();
                    }
                }
            });
        }

        /// <summary>
        /// Start a client connecting to server.
        /// </summary>
        /// <param name="id">Client id used in log.</param>
        /// <param name="port">The server port to connect to.</param>
        private async Task<bool> StartTransportClient(string id, int port)
        {
            bool connected = false;
            await Task.Run(async () =>
            {
                var configuration = new SecureTransport.Configuration()
                {
                    CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                    AllowAutoReconnect = false,
                };
                using (var transport = new SecureTransport(configuration))
                {
                    transport.OnNewConnection = connection =>
                    {
                        connected = true;
                        log($"{id} Connection Established with {connection.RemoteEndPoint}");

                        connection.OnPacketReceived = packet =>
                        {
                            connection.SendAsync(packet);
                            return Task.CompletedTask;
                        };

                        connection.OnConnectionLost = () =>
                        {
                            log($"{id} Connection with {connection.RemoteEndPoint} was lost");
                        };
                    };

                    IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
                    try
                    {
                        await await Task.WhenAny(new Task[2]
                        {
                            // For the failure case, this call will finish after 5sec timeout.
                            transport.StartClient(new IPEndPoint[] { new IPEndPoint(ipAddress, port) }),
                            Task.Delay(10000),
                        });
                    }
                    catch (Exception ex)
                    {
                        log($"{id} Exception: {ex}");
                        connected = false;
                    }
                }
            });
            return connected;
        }
    }
}
