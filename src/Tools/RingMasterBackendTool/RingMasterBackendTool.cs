// <copyright file="RingMasterBackendTool.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.RingMaster.Tools
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    /// <summary>
    /// RingMasterBackend tool hosts an instance RingMasterServerBackend and acts
    /// as a standalone ringmaster server.
    /// </summary>
    public class RingMasterBackendTool
    {
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

            Trace.Listeners.Add(new ConsoleTraceListener());

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var protocol = new RingMasterCommunicationProtocol();
            using (var backend = CreateBackend())
            using (var ringMasterServer = new RingMasterServer(protocol, null, CancellationToken.None))
            {
                var transportConfig = new SecureTransport.Configuration
                {
                    UseSecureConnection = false,
                    IsClientCertificateRequired = false,
                    CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion
                };

                using (var serverTransport = new SecureTransport(transportConfig))
                {
                    ringMasterServer.RegisterTransport(serverTransport);
                    ringMasterServer.OnInitSession = initRequest =>
                    {
                        return new CoreRequestHandler(backend, initRequest);
                    };

                    serverTransport.StartServer(port);

                    Console.WriteLine("Press ENTER to exit...");
                    Console.ReadLine();
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

                return ConfigurationManager.AppSettings[settingName];
            }
            catch
            {
                return null;
            }
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
                Trace.TraceInformation("CreateBackend");

                var factory = new InMemoryFactory();
                RingMasterBackendCore.GetSettingFunction = GetSetting;
                backend = new RingMasterBackendCore(factory);

                backend.StartService = (p1, p2) => { backendStarted.Set(); };
                backend.Start();
                backend.OnBecomePrimary();

                if (backendStarted.Wait(30000))
                {
                    var backendToReturn = backend;
                    backend = null;
                    return backendToReturn;
                }
                else
                {
                    throw new ApplicationException("Backend failed to start");
                }
            }
            finally
            {
                if (backend != null)
                {
                    backend.Dispose();
                }
            }
        }
    }
}