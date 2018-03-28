// <copyright file="MdmHelper.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System.Configuration;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;

    /// <summary>
    /// Perf test operation types
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// The ping ping
        /// </summary>
        PingPong,

        /// <summary>
        /// The create
        /// </summary>
        Create,

        /// <summary>
        /// The set
        /// </summary>
        Set,

        /// <summary>
        /// The get
        /// </summary>
        Get,

        /// <summary>
        /// The delete
        /// </summary>
        Delete,

        /// <summary>
        /// The get full subtree
        /// </summary>
        GetFullSubtree,

        /// <summary>
        /// The multi create
        /// </summary>
        MultiCreate,

        /// <summary>
        /// The batch create
        /// </summary>
        BatchCreate,

        /// <summary>
        /// The bulk watcher create node
        /// </summary>
        BulkWatcherCreateNode,

        /// <summary>
        /// The bulk watcher change node
        /// </summary>
        BulkWatcherChangeNode,

        /// <summary>
        /// The bulk watcher read node
        /// </summary>
        BulkWatcherReadNode,

        /// <summary>
        /// The install bulk watcher
        /// </summary>
        InstallBulkWatcher,

        /// <summary>
        /// The bulk watcher trigger
        /// </summary>
        BulkWatcherTrigger,
    }

    /// <summary>
    /// The Mdm helper
    /// </summary>
    public static class MdmHelper
    {
        private static IMetricsFactory metricsFactory;

        private static bool mdmEnabled = false;

        private static string environment = "test";

        private static string tenant = "localhost";

        private static string mdmAccountName = "SDNPubSub";

        private static IMetric1D operationDurationMetrics;

        private static IMetric1D bytesProcessedMetrics;

        private static IMetric1D watcherCountMatrics;

        /// <summary>
        /// Initializes the Mdm metrics.
        /// </summary>
        /// <param name="roleInstance">The role instance.</param>
        public static void Initialize(string roleInstance)
        {
            if (!bool.TryParse(ConfigurationManager.AppSettings["MdmEnabled"], out mdmEnabled))
            {
                mdmEnabled = false;
            }

            environment = ConfigurationManager.AppSettings["Environment"];
            tenant = ConfigurationManager.AppSettings["Tenant"];
            mdmAccountName = ConfigurationManager.AppSettings["MdmAccountName"];

            if (mdmEnabled)
            {
                IfxInstrumentation.Initialize(MdmConstants.VegaServiceFabricPerfIfxSession, mdmAccountName);
                metricsFactory = IfxInstrumentation.CreateMetricsFactory(mdmAccountName, MdmConstants.MdmNamespace, environment, tenant, string.Empty, roleInstance);

                InitializeMetrics();
                System.Console.WriteLine("MDM enabled");
            }
            else
            {
                System.Console.WriteLine("MDM disaabled");
            }
        }

        /// <summary>
        /// Logs the duration of the operation.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="operationType">Type of the operation.</param>
        public static void LogOperationDuration(long val, OperationType operationType)
        {
            if (mdmEnabled)
            {
                operationDurationMetrics.LogValue(val, operationType.ToString());
            }
        }

        /// <summary>
        /// Logs the bytes processed.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="operationType">Type of the operation.</param>
        public static void LogBytesProcessed(long val, OperationType operationType)
        {
            if (mdmEnabled)
            {
                bytesProcessedMetrics.LogValue(val, operationType.ToString());
            }
        }

        /// <summary>
        /// Logs the watcher count processed.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="operationType">Type of the operation.</param>
        public static void LogWatcherCountProcessed(long val, OperationType operationType)
        {
            if (mdmEnabled)
            {
                watcherCountMatrics.LogValue(val, operationType.ToString());
            }
        }

        private static void InitializeMetrics()
        {
            operationDurationMetrics = metricsFactory.Create1D(MdmConstants.OperationDuration, nameof(OperationType));
            bytesProcessedMetrics = metricsFactory.Create1D(MdmConstants.BytesProcessed, nameof(OperationType));
            watcherCountMatrics = metricsFactory.Create1D(MdmConstants.WatcherCountProcessed, nameof(OperationType));
        }
    }
}
