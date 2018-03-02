// <copyright file="IfxInstrumentation.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation
{
    using System;
    using System.Diagnostics;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Cloud.InstrumentationFramework;

    public static class IfxInstrumentation
    {
        public const uint DefaultMaxTraceFileSizeInKB = 10000;
        public const uint DefaultMaxCompletedTraceFileSizeInMB = 50000;

        public static void Initialize(string sessionName, string monitoringAccountName, string traceDirectory = null)
        {
            if (sessionName == null)
            {
                throw new ArgumentNullException(nameof(sessionName));
            }

            if (monitoringAccountName == null)
            {
                throw new ArgumentNullException(nameof(monitoringAccountName));
            }

            var instrumentationSpec = new InstrumentationSpecification();
            var auditSpec = new AuditSpecification();

            instrumentationSpec.EmitIfxMetricsEvents = true;
            instrumentationSpec.MonitoringAccountName = monitoringAccountName;

            if (traceDirectory != null)
            {
                instrumentationSpec.WriteIfxTracerDiskLogs = true;
                instrumentationSpec.TraceDirectory = traceDirectory;
            }

            instrumentationSpec.MaxSizeTraceFileInKb = DefaultMaxTraceFileSizeInKB;
            instrumentationSpec.MaxSizeCompletedTraceFilesInMb = DefaultMaxCompletedTraceFileSizeInMB;

            IfxInitializer.IfxInitialize(
                sessionName,
                instrumentationSpec,
                auditSpec);
        }

        public static IMetricsFactory CreateMetricsFactory(
            string mdmAccountName,
            string mdmNamespace,
            string environment,
            string tenant,
            string roleName,
            string roleInstanceId)
        {
            return new IfxMetricsFactory(mdmAccountName, mdmNamespace, environment, tenant, roleName, roleInstanceId);
        }

        public static IMetricsFactory CreateMetricsFactory(
            string mdmAccountName,
            string mdmNamespace,
            string[] defaultDimensionNames,
            string[] defaultDimensionValues)
        {
            return new IfxMetricsFactory(mdmAccountName, mdmNamespace, defaultDimensionNames, defaultDimensionValues);
        }

        public static TraceListener CreateTraceListener()
        {
            return new IfxTraceListener();
        }
    }
}