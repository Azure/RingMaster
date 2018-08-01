// <copyright file="OperationType.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
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

        /// <summary>
        /// The LNM vnet publishing scenario
        /// </summary>
        LnmVnetPublishingScenario,
    }
}
