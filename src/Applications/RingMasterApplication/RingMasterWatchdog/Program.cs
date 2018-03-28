// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterWatchdog
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
    /// RingMaster watchdog.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point
        /// </summary>
        public static void Main()
        {
            RingMasterApplicationHelper.AttachDebugger();

            LogFileEventTracing.Start(@"c:\Resources\Directory\RingMasterWatchdog.LogPath");

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

                    RingMasterWatchdogEventSource.Log.ConfigurationSettings(
                        monitoringConfiguration.Environment,
                        monitoringConfiguration.Tenant,
                        monitoringConfiguration.Role,
                        monitoringConfiguration.IfxSession,
                        monitoringConfiguration.MdmAccount);

                    LogFileEventTracing.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-RingMasterWatchdog", EventLevel.Informational, "RingMasterWatchdog");
                    LogFileEventTracing.AddEventSource("Microsoft-ServiceFabric-Services", EventLevel.Informational, "ServiceFabricServices");
                    Trace.Listeners.Add(new LogFileTraceListener());

                    var metricsFactory = IfxInstrumentation.CreateMetricsFactory(
                        monitoringConfiguration.MdmAccount,
                        monitoringConfiguration.MdmNamespace,
                        monitoringConfiguration.Environment,
                        monitoringConfiguration.Tenant,
                        monitoringConfiguration.Role,
                        monitoringConfiguration.RoleInstance);

                    ServiceRuntime.RegisterServiceAsync(
                        "RingMasterWatchdog",
                        serviceContext => new RingMasterWatchdog(serviceContext, metricsFactory)).Wait();
                    RingMasterWatchdogEventSource.Log.RegisterServiceSucceeded();

                    Thread.Sleep(Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    RingMasterWatchdogEventSource.Log.RegisterServiceFailed(ex.ToString());
                    throw;
                }
            }
        }
    }
}
