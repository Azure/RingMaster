// <copyright file="SecureTransportServiceTool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.SecureTransportServiceTool
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    ///   TODO: Comment this region
    /// </summary>
    public class SecureTransportServiceTool
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            if (args == null || args.Length < 1)
            {
                Trace.TraceError("USAGE: SecureTransportServiceTool.exe <port>");
                return;
            }

            int port = int.Parse(args[0]);

            var configuration = new SecureTransport.Configuration();

            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            IConfiguration appSettings = builder.Build();

            int maxConcurrentRequests = int.Parse(appSettings["MaxConcurrentRequests"]);

            configuration.UseSecureConnection = bool.Parse(appSettings["SSL.UseSSL"]);
            configuration.SendBufferSize = int.Parse(appSettings["SendBufferSize"]);
            configuration.ReceiveBufferSize = int.Parse(appSettings["ReceiveBufferSize"]);

            if (configuration.UseSecureConnection)
            {
                string[] clientThumbprints = appSettings["SSL.ClientCerts"].Split(new char[] { ';', ',' });
                string[] serviceThumbprints = appSettings["SSL.ServerCerts"].Split(new char[] { ';', ',' });

                configuration.ClientCertificates = SecureTransport.GetCertificatesFromThumbPrintOrFileName(clientThumbprints);
                configuration.ServerCertificates = SecureTransport.GetCertificatesFromThumbPrintOrFileName(serviceThumbprints);
            }

            Trace.TraceInformation(
                "Listening on port {0}. Using SSL={1}, MaxConcurentRequests={2}, SendBufferSize={3}, ReceiveBufferSize={4}",
                port,
                configuration.UseSecureConnection,
                maxConcurrentRequests,
                configuration.SendBufferSize,
                configuration.ReceiveBufferSize);

            using (var transport = new SecureTransport(configuration))
            {
                var timer = Stopwatch.StartNew();
                long activeConnections = 0;
                long packetsReceived = 0;
                Task serverTask = transport.StartServer(port);

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Trace.TraceInformation("Attempting to close server transport");
                    transport.Close();
                };

                var semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
                transport.OnNewConnection = connection =>
                {
                    Trace.TraceInformation("Connection Established with {0}", connection.RemoteEndPoint);
                    Interlocked.Increment(ref activeConnections);

                    connection.OnPacketReceived = packet =>
                    {
                        semaphore.Wait();
                        Interlocked.Increment(ref packetsReceived);
                        connection.SendAsync(packet).ContinueWith(_ => semaphore.Release());
                    };

                    connection.OnConnectionLost = () =>
                    {
                        Trace.TraceInformation("Connection with {0} was lost", connection.RemoteEndPoint);
                        Interlocked.Decrement(ref activeConnections);
                    };
                };

                while (!serverTask.Wait(5000))
                {
                    timer.Stop();
                    long rate = (long)(packetsReceived * 1000) / timer.ElapsedMilliseconds;
                    int inflightCount = maxConcurrentRequests - semaphore.CurrentCount;
                    Trace.TraceInformation($"ActiveConnections={activeConnections}, RequestRate {rate} InFlight count={inflightCount}");
                    packetsReceived = 0;
                    timer.Restart();
                }
            }
        }
    }
}
