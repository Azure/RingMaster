// <copyright file="CallbackWatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;

    /// <summary>
    /// Use the RingMaster watcher via the callback in the test method
    /// </summary>
    public class CallbackWatcher : IWatcher
    {
        /// <summary>
        /// Gets or sets the unique id of this watcher.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets a value indicating whether the watcher is for a single use only.
        /// </summary>
        public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

        /// <summary>
        /// Gets the kind of the watcher, if it is for single use and if the data is included on notification
        /// </summary>
        public WatcherKind Kind
        {
            get
            {
                return WatcherKind.IncludeData;
            }
        }

        /// <summary>
        /// Gets or sets the delegate for processing the watcher event
        /// </summary>
        /// <value>
        /// The on process.
        /// </value>
        public Action<WatchedEvent> OnProcess { get; set; } = null;

        /// <summary>
        /// Processes the specified event.
        /// </summary>
        /// <param name="evt">The event</param>
        public void Process(WatchedEvent evt)
        {
            this.OnProcess?.Invoke(evt);
        }
    }
}
