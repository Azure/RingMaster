// <copyright file="Program.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
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
        /// <param name="args">Arguments provided to the program</param>
        public static void Main(string[] args)
        {
            RingMasterApplicationHelper.AttachDebugger();

            LogFileEventTracing.Start(@"c:\Resources\Directory\RingMasterService.LogPath");

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

                    var level = EventLevel.Informational;
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-RingMasterService", level, "RingMasterService");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Backend-RingMasterEvents", level, "RingMasterBackendCore");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence", level, "Persistence");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence-ServiceFabric", level, "ServiceFabricPersistence");
                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-SecureTransport", level, "SecureTransport");
                    LogFileEventTracing.AddEventSource("Microsoft-ServiceFabric-Services", level, "ServiceFabricServices");
                    Trace.Listeners.Add(new LogFileTraceListener());

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
