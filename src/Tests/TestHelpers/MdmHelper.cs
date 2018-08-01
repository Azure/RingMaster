// <copyright file="MdmHelper.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;

    /// <summary>
    /// The Mdm helper
    /// </summary>
    public static class MdmHelper
    {
        private static IMetricsFactory metricsFactory;

        private static bool mdmEnabled = false;

        private static IMetric1D operationDurationMetrics;

        private static IMetric1D bytesProcessedMetrics;

        private static IMetric1D watcherCountMatrics;

        /// <summary>
        /// Initializes the specified environment.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="tenant">The tenant.</param>
        /// <param name="mdmAccountName">Name of the MDM account.</param>
        /// <param name="roleInstance">The role instance.</param>
        /// <param name="roleName">Name of the role.</param>
        /// <param name="sessionName">Name of the session.</param>
        /// <param name="mdmNamespace">The MDM namespace.</param>
        /// <param name="enableMdm">if set to <c>true</c> [MDM enabled].</param>
        public static void Initialize(string environment, string tenant, string mdmAccountName, string roleInstance, string roleName, string sessionName, string mdmNamespace, bool enableMdm)
        {
            mdmEnabled = enableMdm;

            if (mdmEnabled)
            {
                IfxInstrumentation.Initialize(sessionName, mdmAccountName);
                metricsFactory = IfxInstrumentation.CreateMetricsFactory(mdmAccountName, mdmNamespace, environment, tenant, roleName, roleInstance);

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
