// <copyright file="KeeperState.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Watcher.Event
{
    /// <summary>
    /// Enum KeeperState
    /// </summary>
    public enum KeeperState
    {
        /// <summary>
        /// The unknown
        /// </summary>
        /// <remarks>
        /// Deprecated
        /// </remarks>
        Unknown = 0,

        /// <summary>
        /// The authentication failed
        /// </summary>
        AuthFailed,

        /// <summary>
        /// The client is in the disconnected state - it is not connected to any server in the ensemble.
        /// </summary>
        Disconnected,

        /// <summary>
        /// The serving cluster has expired this session.
        /// </summary>
        Expired,

        /// <summary>
        /// The no synchronize connected (deprecated).
        /// </summary>
        NoSyncConnected,

        /// <summary>
        /// The client is in the connected state - it is connected to a server in the ensemble
        /// (one of the servers specified in the host connection parameter during ZooKeeper
        /// client creation).
        /// </summary>
        SyncConnected,
    }
}
