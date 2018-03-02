// <copyright file="SecureTransportClientTool.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.SecureTransportClientTool
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    public class SecureTransportClientTool
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            if (args == null || args.Length < 1)
            {
                Trace.TraceError("USAGE: SecureTransportClientTool.exe <connection string> [<request length>]");
                return;
            }

            string connectionString = args[0];
            int requestLength = 128;
            int waitTimeoutMs = 30000;

            if (args.Length > 1)
            {
                requestLength = int.Parse(args[1]);
            }

            var configuration = new SecureTransport.Configuration();
            int maxConcurrentRequests = int.Parse(ConfigurationManager.AppSettings["MaxConcurrentRequests"]);
            configuration.SendBufferSize = int.Parse(ConfigurationManager.AppSettings["SendBufferSize"]);
            configuration.ReceiveBufferSize = int.Parse(ConfigurationManager.AppSettings["ReceiveBufferSize"]);

            configuration.UseSecureConnection = bool.Parse(ConfigurationManager.AppSettings["SSL.UseSSL"]);
            if (configuration.UseSecureConnection)
            {
                string[] clientThumbprints = ConfigurationManager.AppSettings["SSL.ClientCerts"].Split(new char[] { ';', ',' });
                string[] serviceThumbprints = ConfigurationManager.AppSettings["SSL.ServerCerts"].Split(new char[] { ';', ',' });

                configuration.ClientCertificates = SecureTransport.GetCertificatesFromThumbPrintOrFileName(clientThumbprints);
                configuration.ServerCertificates = SecureTransport.GetCertificatesFromThumbPrintOrFileName(serviceThumbprints);
            }

            SecureTransport.TraceLevel = (TraceLevel)Enum.Parse(typeof(TraceLevel), ConfigurationManager.AppSettings["TraceLevel"]);

            Trace.TraceInformation(
                "Connecting to {0}.  Using SSL={1} RequestLength={2} MaxConcurrentRequests={3}, SendBufferSize={4}, ReceiveBufferSize={5}",
                connectionString,
                configuration.UseSecureConnection,
                requestLength,
                maxConcurrentRequests,
                configuration.SendBufferSize,
                configuration.ReceiveBufferSize);

            var connectionAvailable = new ManualResetEvent(false);

            IPEndPoint[] endpoints = SecureTransport.ParseConnectionString(connectionString);

            using (var transport = new SecureTransport(configuration))
            {
                transport.StartClient(endpoints);

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Trace.TraceInformation("Attempting to close client transport");
                    transport.Close();
                };

                IConnection currentConnection = null;
                transport.OnNewConnection = connection =>
                {
                    currentConnection = connection;
                    connectionAvailable.Set();
                };

                transport.OnConnectionLost = () =>
                {
                    connectionAvailable.Reset();
                };

                var random = new Random();
                var timer = new Stopwatch();
                timer.Start();
                long requestsSent = 0;

                byte[] request = new byte[requestLength];
                random.NextBytes(request);

                var sendSemaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);

                while (true)
                {
                    if (!connectionAvailable.WaitOne(waitTimeoutMs))
                    {
                        Trace.TraceWarning("Connection is not available. retrying...");
                        continue;
                    }

                    currentConnection.OnPacketReceived = packet =>
                    {
                    };

                    try
                    {
                        if (!sendSemaphore.Wait(waitTimeoutMs))
                        {
                            Trace.TraceError("Timedout waiting for send semaphore...");
                            continue;
                        }

                        currentConnection.SendAsync(request).ContinueWith(_ => sendSemaphore.Release());
                    }
                    catch (IOException ex)
                    {
                        Trace.TraceError("IO Exception: {0}", ex.Message);
                    }

                    Interlocked.Increment(ref requestsSent);
                    if (timer.ElapsedMilliseconds > 5000)
                    {
                        long rate = (requestsSent * 1000) / timer.ElapsedMilliseconds;
                        int inflightCount = maxConcurrentRequests - sendSemaphore.CurrentCount;
                        Trace.TraceInformation($"Send rate={rate} InFlight={inflightCount}");
                        requestsSent = 0;
                        timer.Restart();
                    }
                }
            }
        }
    }
}