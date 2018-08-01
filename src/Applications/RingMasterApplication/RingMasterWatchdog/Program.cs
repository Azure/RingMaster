// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterWatchdog
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using System.IO;
    using System.Threading;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.Extensions.Configuration;
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
            var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");
            IConfiguration appSettings = builder.Build();

            RingMasterApplicationHelper.AttachDebugger(int.Parse(appSettings["DebuggerAttachTimeout"]));

            LogFileEventTracing.Start(Path.Combine(appSettings["LogFolder"], "RingMasterWatchdog.LogPath"));

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
