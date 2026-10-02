// <copyright file="LogFileEventTracer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Reflection;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;

    /// <summary>
    /// Helper adapter that allows switching between various implementations of log file tracers.
    /// </summary>
    internal class LogFileEventTracer
    {
        private readonly bool useV2;
        private readonly string logDirectory;
        private readonly int logFileSize;

        public LogFileEventTracer(bool useV2, string logDirectory, int logFileSize)
        {
            this.useV2 = useV2;
            this.logDirectory = logDirectory;
            this.logFileSize = logFileSize;

            this.Start();

            Trace.Listeners.Add(new LogFileTraceListener(useV2));
        }

        /// <summary>
        /// Stops listening events.
        /// </summary>
        public void Stop()
        {
            if (this.useV2)
            {
                LogFileEventTracingV2.Stop();
            }
            else
            {
                LogFileEventTracing.Stop();
            }
        }

        /// <summary>
        /// Register all known event sources for tracing the logs into the files.
        /// </summary>
        public void RegisterAllEventSources()
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

            this.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-RingMasterService", level, "RingMasterService");

            // For v2 tracer we also disabling one very chatty event that makes the file traces not usable: we could have up to 10B of them in a day!
            EventKeywords coreKeywords = this.useV2 ? RingMasterEventSourceKeywords.ExcludeProcessMessage : EventKeywords.None;
            this.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Backend-RingMasterEvents", level, "RingMasterBackendCore", keywords: coreKeywords);
            this.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence", EventLevel.Warning, "Persistence");
            this.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence-ServiceFabric", level, "ServiceFabricPersistence");
            this.AddEventSource("Microsoft-Azure-Networking-Infrastructure-RingMaster-SecureTransport", level, "SecureTransport");
        }

        private void Start()
        {
            if (this.useV2)
            {
                LogFileEventTracingV2.Start(this.logDirectory, logFileSize: this.logFileSize);
            }
            else
            {
                LogFileEventTracing.Start(this.logDirectory, logFileSize: this.logFileSize);
            }
        }

        private bool AddEventSource(string eventSourceName, EventLevel level, string shortName, EventKeywords keywords = EventKeywords.None)
        {
            if (this.useV2)
            {
                return LogFileEventTracingV2.AddEventSource(eventSourceName, level, shortName, keywords);
            }
            else
            {
                return LogFileEventTracing.AddEventSource(eventSourceName, level, shortName, keywords);
            }
        }
    }
}
