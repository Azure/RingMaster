// <copyright file="TransportBench.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    /// <summary>
    /// Transport layer benchmark
    /// </summary>
    internal sealed class TransportBench : IBench
    {
        private const int PacketLength = 1024;
        private const int Port = 12345;
        private const int WarmupMilliSeconds = 60_000;
        private const int MeasureMilliSeconds = 60_000;
        private const int PrintStatusInterval = 5_000;

        /// <inheritdoc />
        public void Run(Action<string> log)
        {
            log($"## Transport Benchmark (transfering network packet with {PacketLength} bytes packet)\n");

            SocketNetworkStreamInternal(log, "SocketNetworkStreamPingPong", 1).GetAwaiter().GetResult();
            SocketNetworkStreamInternal(log, "SocketNetworkStreamThroughput", 100).GetAwaiter().GetResult();

            SecureTransportInternal(log, "SecureTransportPingPong", 1);
            SecureTransportInternal(log, "SecureTransportThroughput", 100);
        }

        private static void SecureTransportInternal(Action<string> log, string testCaseName, int numberOfClients)
        {
            var packetCount = 0;

            log($"Starting {testCaseName} ...");

            SecureTransport server = null;
            SecureTransport[] clients = new SecureTransport[0];

            try
            {
                // Start the service
                server = new SecureTransport(new SecureTransport.Configuration
                {
                    UseSecureConnection = false,
                    IsClientCertificateRequired = false,
                    CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                });

                var serverSendTask = Task.CompletedTask;

                server.OnNewConnection = connection =>
                {
                    connection.OnPacketReceived = packet =>
                    {
                        Interlocked.Increment(ref packetCount);
                        serverSendTask.GetAwaiter().GetResult();
                        serverSendTask = connection.SendAsync(packet);
                    };

                    Trace.TraceInformation("Server accepted a new connection: {0}", connection.RemoteIdentity);
                };

                server.StartServer(Port);

                // Start the client
                clients = Enumerable.Range(0, numberOfClients).Select(
                    _ => new SecureTransport(new SecureTransport.Configuration
                    {
                        UseSecureConnection = false,
                        IsClientCertificateRequired = false,
                        CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                    }))
                    .ToArray();

                Parallel.ForEach(
                    clients,
                    client => client.OnNewConnection = connection =>
                    {
                        var clientSendTask = Task.CompletedTask;

                        connection.OnPacketReceived = packet =>
                        {
                            clientSendTask.GetAwaiter().GetResult();
                            clientSendTask = connection.SendAsync(packet);
                        };

                        clientSendTask = connection.SendAsync(new byte[PacketLength]);
                    });

                Parallel.ForEach(
                    clients,
                    client => client.StartClient(new IPEndPoint(IPAddress.Loopback, Port)));

                log($"    Warming up for {WarmupMilliSeconds / 1000} seconds");
                Thread.Sleep(WarmupMilliSeconds);

                log($"    Start measuring for {MeasureMilliSeconds} seconds");
                var sw = new Stopwatch();
                int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
                sw.Start();
                Interlocked.Exchange(ref packetCount, 0);

                for (int i = 0; i < MeasureMilliSeconds / PrintStatusInterval; i++)
                {
                    Thread.Sleep(PrintStatusInterval);
                    log($"    {DateTime.Now.ToString()} count={packetCount}");
                }

                sw.Stop();
                var totalCount = packetCount;
                var rate = totalCount / sw.Elapsed.TotalSeconds;

                Parallel.ForEach(clients, client => client.Close());
                server.Close();
                log($"{testCaseName}: {totalCount} in {sw.Elapsed} with {numberOfClients} clients. QPS={rate}");
                log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
                log(string.Empty);
            }
            finally
            {
                server?.Dispose();
                Parallel.ForEach(clients, client => client.Dispose());
            }
        }

        private static async Task SocketNetworkStreamInternal(Action<string> log, string testCaseName, int numberOfClients)
        {
            var stop = false;
            var packetCount = 0;

            var listener = default(Socket);
            var clients = default(Socket[]);
            var clientStreams = default(NetworkStream[]);

            log($"Starting {testCaseName} ...");

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clients = Enumerable.Range(0, numberOfClients)
                    .Select(_ => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    .ToArray();

                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(numberOfClients);

                _ = Task.Run(() =>
                {
                    while (!stop)
                    {
                        var server = listener.Accept();
                        Task.Run(async () =>
                        {
                            NetworkStream serverStream = null;
                            try
                            {
                                serverStream = new NetworkStream(server);

                                var serverData = new byte[PacketLength];
                                var writeTask = Task.CompletedTask;

                                while (!stop)
                                {
                                    var count = await serverStream.ReadAsync(serverData, 0, serverData.Length);
                                    await writeTask;
                                    writeTask = serverStream.WriteAsync(serverData, 0, count);

                                    Interlocked.Increment(ref packetCount);
                                }
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                serverStream?.Dispose();
                                server.Dispose();
                            }
                        });
                    }
                });

                await Task.WhenAll(clients.Select(c => Task.Run(() => c.Connect(listener.LocalEndPoint))));
                clientStreams = clients.Select(c => new NetworkStream(c)).ToArray();

                var clientCount = 0;
                var clientTasks = clientStreams.Select(clientStream => Task.Run(async () =>
                {
                    var count = 0;
                    var clientData = new byte[PacketLength];
                    new Random().NextBytes(clientData);

                    Interlocked.Increment(ref clientCount);

                    try
                    {
                        var readTask = Task.CompletedTask;
                        while (!stop)
                        {
                            await clientStream.WriteAsync(clientData, 0, clientData.Length);
                            await readTask;
                            readTask = clientStream.ReadAsync(clientData, 0, clientData.Length);
                            count++;
                        }
                    }
                    catch (Exception)
                    {
                    }
                })).ToArray();

                log($"    Warming up for {WarmupMilliSeconds / 1000} seconds. #Clients={clientCount}");
                Thread.Sleep(WarmupMilliSeconds);

                log($"    Start measuring for {MeasureMilliSeconds} seconds #Clients={clientCount}");
                var sw = new Stopwatch();
                int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
                Interlocked.Exchange(ref packetCount, 0);
                sw.Start();

                for (int i = 0; i < MeasureMilliSeconds / PrintStatusInterval; i++)
                {
                    Thread.Sleep(PrintStatusInterval);
                    log($"    {DateTime.Now.ToString()} count={packetCount}");
                }

                sw.Stop();
                var totalCount = packetCount;

                log($"    Stopping all clients");
                stop = true;
                Parallel.ForEach(clients, c => c.Shutdown(SocketShutdown.Both));
                await Task.WhenAll(clientTasks);

                var qps = totalCount / sw.Elapsed.TotalSeconds;
                log($"{testCaseName}: {totalCount} with {numberOfClients} clients in {sw.Elapsed}. QPS={qps}");
                log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
            }
            finally
            {
                Parallel.ForEach(clientStreams, cs => cs?.Dispose());
                Parallel.ForEach(clients, client => client?.Close());

                if (listener != null)
                {
                    try
                    {
                        listener.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        listener.Close();
                    }
                }
            }
        }
    }
}
