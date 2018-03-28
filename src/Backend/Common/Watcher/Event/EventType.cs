// <copyright file="EventType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Watcher.Event
{
    /// <summary>
    /// Enum EventType
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0,

        /// <summary>
        /// The node children changed
        /// </summary>
        NodeChildrenChanged,

        /// <summary>
        /// The node created
        /// </summary>
        NodeCreated,

        /// <summary>
        /// The node data changed
        /// </summary>
        NodeDataChanged,

        /// <summary>
        /// The node deleted
        /// </summary>
        NodeDeleted,

        /// <summary>
        /// The watcher removed
        /// </summary>
        WatcherRemoved,
    }
}
