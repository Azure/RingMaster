// <copyright file="RingMasterBackendTool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.RingMaster.Tools
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// RingMasterBackend tool hosts an instance RingMasterServerBackend and acts
    /// as a standalone ringmaster server.
    /// </summary>
    public sealed class RingMasterBackendTool : IDisposable
    {
        private static IConfiguration appSettings;
        private InMemoryFactory factory;
        private RingMasterBackendCore backend;
        private RingMasterServer ringMasterServer;
        private bool disposed = false;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine("USAGE: RingMasterBackend <port>");
                return;
            }

            ushort port = ushort.Parse(args[0]);
            Console.WriteLine("Port={0}", port);

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            using (var p = new RingMasterBackendTool())
            {
                p.Run(port);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (this.factory != null)
                {
                    this.factory.Dispose();
                    this.factory = null;
                }

                if (this.backend != null)
                {
                    this.backend.Dispose();
                    this.backend = null;
                }

                if (this.ringMasterServer != null)
                {
                    this.ringMasterServer.Dispose();
                    this.ringMasterServer = null;
                }
            }
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Information about the unhandled exception</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled Exception {0}", e.ExceptionObject);
            Console.Out.Flush();
            Environment.Exit(1);
        }

        /// <summary>
        /// Gets the value of the given setting either from environment variables or from application configuration.
        /// </summary>
        /// <param name="settingName">Name of the setting</param>
        /// <returns>Value of the setting</returns>
        private static string GetSetting(string settingName)
        {
            try
            {
                // Environment variable takes precendence over application setting
                string environmentSettingValue = Environment.GetEnvironmentVariable(settingName);
                if (environmentSettingValue != null)
                {
                    return environmentSettingValue;
                }

                return appSettings[settingName];
            }
            catch
            {
                return null;
            }
        }

        private void Run(int port)
        {
            var protocol = new RingMasterCommunicationProtocol();
            this.CreateBackend();

            using (var cancel = new CancellationTokenSource())
            {
                this.ringMasterServer = new RingMasterServer(protocol, null, cancel.Token);

                var transportConfig = new SecureTransport.Configuration
                {
                    UseSecureConnection = false,
                    IsClientCertificateRequired = false,
                    CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
                };

                using (var serverTransport = new SecureTransport(transportConfig))
                {
                    this.ringMasterServer.RegisterTransport(serverTransport);
                    this.ringMasterServer.OnInitSession = initRequest =>
                    {
                        return new CoreRequestHandler(this.backend, initRequest);
                    };

                    serverTransport.StartServer(port);

                    Console.WriteLine("Press ENTER to exit...");
                    Console.ReadLine();
                    cancel.Cancel();
                }
            }
        }

        /// <summary>
        /// Creates a new backend with an in-memory store
        /// </summary>
        private void CreateBackend()
        {
            var backendStarted = new ManualResetEventSlim();
            Trace.TraceInformation("CreateBackend");

            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            appSettings = builder.Build();
            RingMasterBackendCore.GetSettingFunction = GetSetting;
            this.factory = new InMemoryFactory();
            this.backend = new RingMasterBackendCore(this.factory);
            this.backend.StartService = (p1, p2) => { backendStarted.Set(); };
            this.backend.Start(CancellationToken.None);
            this.backend.OnBecomePrimary();

            if (!backendStarted.Wait(30000))
            {
                throw new RingMasterBackendException("Backend failed to start");
            }
        }

        /// <summary>
        /// General exception related to the backend
        /// </summary>
        [Serializable]
        public class RingMasterBackendException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RingMasterBackendException"/> class.
            /// </summary>
            /// <param name="message">Exception message</param>
            public RingMasterBackendException(string message)
                : base(message)
            {
            }
        }
    }
}
