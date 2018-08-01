// <copyright file="Program.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.EnumerationStressService
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Reflection;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// Enumeration stress service.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">Arguments provided to the program</param>
        public static void Main(string[] args)
        {
            Trace.Listeners.Add(IfxInstrumentation.CreateTraceListener());

            using (FabricRuntime fabricRuntime = FabricRuntime.Create())
            {
                try
                {
                    var monitoringConfiguration = new MonitoringConfiguration(FabricRuntime.GetActivationContext());

                    IfxInstrumentation.Initialize(monitoringConfiguration.IfxSession, monitoringConfiguration.MdmAccount);

                    EnumerationStressServiceEventSource.Log.ConfigurationSettings(
                        monitoringConfiguration.Environment,
                        monitoringConfiguration.Tenant,
                        monitoringConfiguration.Role,
                        monitoringConfiguration.IfxSession,
                        monitoringConfiguration.MdmAccount);

                    var metricsFactory = IfxInstrumentation.CreateMetricsFactory(
                        monitoringConfiguration.MdmAccount,
                        monitoringConfiguration.MdmNamespace,
                        monitoringConfiguration.Environment,
                        monitoringConfiguration.Tenant,
                        monitoringConfiguration.Role,
                        monitoringConfiguration.RoleInstance);

                    ServiceRuntime.RegisterServiceAsync("EnumerationStressService", serviceContext => new EnumerationStressService(serviceContext, metricsFactory)).Wait();
                    EnumerationStressServiceEventSource.Log.RegisterServiceSucceeded();

                    Assembly assembly = Assembly.GetExecutingAssembly();
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    string version = fvi.FileVersion;
                    var uptime = Stopwatch.StartNew();

                    while (true)
                    {
                        EnumerationStressServiceEventSource.Log.ReportServiceStatus(version, (long)uptime.Elapsed.TotalSeconds);
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                    }
                }
                catch (Exception ex)
                {
                    EnumerationStressServiceEventSource.Log.RegisterServiceFailed(ex.ToString());
                    throw;
                }
            }
        }
    }
}
