// <copyright file="MdmConstants.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
    /// <summary>
    /// The Mdm Constants class.
    /// </summary>
    public static class MdmConstants
    {
        /// <summary>
        /// The operation duration
        /// </summary>
        public const string OperationDuration = nameof(OperationDuration);

        /// <summary>
        /// The operation throughput
        /// </summary>
        public const string BytesProcessed = nameof(BytesProcessed);

        /// <summary>
        /// The watcher count processed
        /// </summary>
        public const string WatcherCountProcessed = nameof(WatcherCountProcessed);

        /// <summary>
        /// The MDM namespace
        /// </summary>
        public const string ServiceFabricPerfMdmNamespace = "VegaPerf";

        /// <summary>
        /// The distributed perf MDM namespace
        /// </summary>
        public const string DistributedPerfMdmNamespace = "VegaDistPerf";

        /// <summary>
        /// The vega service fabric perf ifx session
        /// </summary>
        public const string VegaServiceFabricPerfIfxSession = nameof(VegaServiceFabricPerfIfxSession);

        /// <summary>
        /// The vega distributed perf ifx session
        /// </summary>
        public const string VegaDistributedPerfIfxSession = nameof(VegaDistributedPerfIfxSession);
    }
}
