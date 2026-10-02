// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Fabric;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterApplication.Utilities;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.Extensions.Configuration;
    using Microsoft.ServiceFabric.Services.Runtime;

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

            LogFileEventTracer tracer = CreateTracer(appSettings);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit +=
                (sender, eventArgs) =>
                {
                    tracer.Stop();
                };

            TaskScheduler.UnobservedTaskException +=
                (sender, eventArgs) =>
                {
                    Trace.TraceError($"RingMasterService.UnobservedTaskException. {eventArgs.Exception}");
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

                    tracer.RegisterAllEventSources();

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

        private static LogFileEventTracer CreateTracer(IConfiguration appSettings)
        {
            bool useLegacyLogFileTracer = UseLegacyLogFileTracer(appSettings);
            int logFileSize = GetLogFileSize(appSettings);
            string logDirectory = Path.Combine(Environment.GetEnvironmentVariable("RINGMASTER_LOG_PATH").ThrowIfNull(), "RingMasterService.LogPath");

            return new LogFileEventTracer(useV2: !useLegacyLogFileTracer, logDirectory, logFileSize);
        }

        private static bool UseLegacyLogFileTracer(IConfiguration appSettings)
        {
            const bool defaultValue = false;
            const string settingName = "UseLegacyLogFileTracer";

            try
            {
                var settingAsString = appSettings[settingName];
                if (settingAsString == null)
                {
                    return defaultValue;
                }

                return bool.Parse(settingAsString);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"RingMasterService: failing to get {settingName}. Using the defaults. " + ex);
                return defaultValue;
            }
        }

        private static int GetLogFileSize(IConfiguration appSettings)
        {
            const int DefaultLogFileSize = 50 * 1024 * 1024;
            try
            {
                var logFileSizeAsString = appSettings["LogFileSizeInMb"];
                if (logFileSizeAsString == null)
                {
                    return DefaultLogFileSize;
                }

                return int.Parse(logFileSizeAsString) * 1024 * 1024;
            }
            catch (Exception ex)
            {
                Trace.TraceError("RingMasterService: failing to get the log file size from the config. Using the defaults. " + ex);
                return DefaultLogFileSize;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError($"RingMasterService.UnhandledException exception={e.ExceptionObject}, isTerminating={e.IsTerminating}");
        }
    }
}
