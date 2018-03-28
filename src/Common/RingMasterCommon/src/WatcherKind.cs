// <copyright file="WatcherKind.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;

    /// <summary>
    /// Types of watcher
    /// </summary>
    [Flags]
    public enum WatcherKind : byte
    {
        /// <summary>
        /// If the watcher is for single use
        /// </summary>
        OneUse = 0x01,

        /// <summary>
        /// If the watcher notification includes data or children list
        /// </summary>
        IncludeData = 0x02,
    }
}
