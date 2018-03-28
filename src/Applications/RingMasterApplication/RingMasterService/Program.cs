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
    using System.Reflection;
    using System.Threading;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
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
            RingMasterApplicationHelper.AttachDebugger();

            LogFileEventTracing.Start(@"c:\Resources\Directory\RingMasterService.LogPath");
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

                    // Ensure the event source is loaded
                    Assembly.GetAssembly(typeof(AbstractPersistedDataFactory))
                        .GetType("Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.PersistenceEventSource")
                        ?.GetProperty("Log")
                        ?.GetValue(null);

                    var level = EventLevel.Informational;
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-RingMasterService", level, "RingMasterService");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Backend-RingMasterEvents", level, "RingMasterBackendCore");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence", EventLevel.Warning, "Persistence");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence-ServiceFabric", level, "ServiceFabricPersistence");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-SecureTransport", level, "SecureTransport");
                    LogFileEventTracing.AddEventSource("Microsoft-ServiceFabric-Services", level, "ServiceFabricServices");

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

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError($"RingMasterService.UnhandledException exception={e.ExceptionObject}, isTerminating={e.IsTerminating}");
        }
    }
}
