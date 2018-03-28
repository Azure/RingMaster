// <copyright file="WatcherCall.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;

    /// <summary>
    /// Represents a watcher call.
    /// </summary>
    [Serializable]
    public class WatcherCall
    {
        /// <summary>
        /// Gets or sets the watcher identifier.
        /// </summary>
        public ulong WatcherId { get; set; }

        /// <summary>
        /// Gets or sets the kind of the watcher, if it is for single use or includes data.
        /// </summary>
        public WatcherKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the watcher.
        /// </summary>
        /// <value>The watcher.</value>
        public IWatcher Watcher { get; set; }

        /// <summary>
        /// Gets or sets the watcher event.
        /// </summary>
        public WatchedEvent WatcherEvt { get; set; }
    }
}