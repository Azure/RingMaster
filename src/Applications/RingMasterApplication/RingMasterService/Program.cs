// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.Extensions.Configuration;
    using Microsoft.ServiceFabric.Services.Runtime;
    using RingMasterApplication.Utilities;

    /// <summary>
    /// RingMaster service.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point
        /// </summary>
        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:DisposeObjectsBeforeLosingScope",
            Scope = "method",
            Target = "Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService.Program.Main()",
            Justification = "Object will be disposed when the service is unloaded")]
        public static void Main()
        {
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            IConfiguration appSettings = builder.Build();

            RingMasterApplicationHelper.AttachDebugger(int.Parse(appSettings["DebuggerAttachTimeout"]));

            LogFileEventTracing.Start(Path.Combine(appSettings["LogFolder"], "RingMasterService.LogPath"));
            Trace.Listeners.Add(new LogFileTraceListener());

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit +=
                (sender, eventArgs) =>
                {
                    LogFileEventTracing.Stop();
                };

            using (FabricRuntime fabricRuntime = FabricRuntime.Create())
            {
                try
                {
                    var monitoringConfiguration = new MonitoringConfiguration(FabricRuntime.GetActivationContext());

                    IfxInstrumentation.Initialize(monitoringConfiguration.IfxSession, monitoringConfiguration.MdmAccount);

                    RingMasterServiceEventSource.Log.ConfigurationSettings(
                        monitoringConfiguration.Environment,
                        monitoringConfiguration.Tenant,
                        monitoringConfiguration.Role,
                        monitoringConfiguration.IfxSession,
                        monitoringConfiguration.MdmAccount);

                    AddAllEventSources();

                    var ringMasterMetricsFactory = IfxInstrumentation.CreateMetricsFactory(
                        monitoringConfiguration.MdmAccount,
                        monitoringConfiguration.MdmNamespace,
                        monitoringConfiguration.Environment,
                        monitoringConfiguration.Tenant,
                        monitoringConfiguration.Role,
                        monitoringConfiguration.RoleInstance);

                    var persistenceMetricsFactory = IfxInstrumentation.CreateMetricsFactory(
                        monitoringConfiguration.MdmAccount,
                        $"{monitoringConfiguration.MdmNamespace}/WinFabPersistence",
                        monitoringConfiguration.Environment,
                        monitoringConfiguration.Tenant,
                        monitoringConfiguration.Role,
                        monitoringConfiguration.RoleInstance);

                    ServiceRuntime.RegisterServiceAsync(
                        "RingMasterService",
                        serviceContext => new RingMasterService(serviceContext, ringMasterMetricsFactory, persistenceMetricsFactory)).Wait();
                    RingMasterServiceEventSource.Log.RegisterServiceSucceeded();

                    Thread.Sleep(Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    RingMasterServiceEventSource.Log.RegisterServiceFailed(ex.ToString());
                    throw;
                }
            }
        }

        private static void AddAllEventSources()
        {
            // Ensure the event source is loaded
            Assembly.GetAssembly(typeof(Backend.RingMasterBackendCore))
                .GetType("Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterEventSource")
                ?.GetProperty("Log")
                ?.GetValue(null);
            Assembly.GetAssembly(typeof(AbstractPersistedDataFactory))
                .GetType("Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.PersistenceEventSource")
                ?.GetProperty("Log")
                ?.GetValue(null);
            Assembly.GetAssembly(typeof(WinFabPersistence.PersistedData))
                .GetType("Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric.ServiceFabricPersistenceEventSource")
                ?.GetProperty("Log")
                ?.GetValue(null);
            Assembly.GetAssembly(typeof(Transport.SecureTransport))
                .GetType("Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport.SecureTransportEventSource")
                ?.GetProperty("Log")
                ?.GetValue(null);

            var level = EventLevel.Informational;
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-RingMasterService", level, "RingMasterService");
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Backend-RingMasterEvents", level, "RingMasterBackendCore");
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence", EventLevel.Warning, "Persistence");
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence-ServiceFabric", level, "ServiceFabricPersistence");
            LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-SecureTransport", level, "SecureTransport");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError($"RingMasterService.UnhandledException exception={e.ExceptionObject}, isTerminating={e.IsTerminating}");
        }
    }
}
