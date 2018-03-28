// <copyright file="IfxInstrumentation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation
{
    using System;
    using System.Diagnostics;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    /// Instrumention using IFx for using MDM, tracing, etc.
    /// </summary>
    public static class IfxInstrumentation
    {
        /// <summary>
        /// The default trace file size in KB
        /// </summary>
        public const uint DefaultMaxTraceFileSizeInKB = 10000;

        /// <summary>
        /// The default completed trace file size in MB
        /// </summary>
        public const uint DefaultMaxCompletedTraceFileSizeInMB = 50000;

        /// <summary>
        /// Initializes the IFx session
        /// </summary>
        /// <param name="sessionName">Name of the IFx session</param>
        /// <param name="monitoringAccountName">Name of the IFx monitoring account</param>
        /// <param name="traceDirectory">Directory to store the trace files</param>
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

        /// <summary>
        /// Creates an metrics factory object
        /// </summary>
        /// <param name="mdmAccountName">Name of MDM account</param>
        /// <param name="mdmNamespace">Namespace of MDM</param>
        /// <param name="environment">Environment name, prod or test</param>
        /// <param name="tenant">Tenant name</param>
        /// <param name="roleName">Role name (not in use)</param>
        /// <param name="roleInstanceId">Role instance name, or node name</param>
        /// <returns>Metrics factory object</returns>
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

        /// <summary>
        /// Creates an metrics factory object
        /// </summary>
        /// <param name="mdmAccountName">Name of MDM account</param>
        /// <param name="mdmNamespace">Namespace of MDM</param>
        /// <param name="defaultDimensionNames">List of default dimension names</param>
        /// <param name="defaultDimensionValues">List of default dimension values</param>
        /// <returns>Metrics factory object</returns>
        public static IMetricsFactory CreateMetricsFactory(
            string mdmAccountName,
            string mdmNamespace,
            string[] defaultDimensionNames,
            string[] defaultDimensionValues)
        {
            return new IfxMetricsFactory(mdmAccountName, mdmNamespace, defaultDimensionNames, defaultDimensionValues);
        }

        /// <summary>
        /// Creates a trace listener
        /// </summary>
        /// <returns>Trace listener object</returns>
        public static TraceListener CreateTraceListener()
        {
            return new IfxTraceListener();
        }
    }
}