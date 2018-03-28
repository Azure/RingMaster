// <copyright file="CachedKind.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;

    /// <summary>
    /// The kind of cached data
    /// </summary>
    [Flags]
    public enum CachedKind : byte
    {
        /// <summary>
        /// no data
        /// </summary>
        None = 0,

        /// <summary>
        /// node data
        /// </summary>
        NodeData = 1,

        /// <summary>
        /// node IStat
        /// </summary>
        NodeStats = 2,

        /// <summary>
        /// node children
        /// </summary>
        NodeChildren = 4,

        /// <summary>
        /// node ACLs
        /// </summary>
        NodeAcls = 8,
    }
}
