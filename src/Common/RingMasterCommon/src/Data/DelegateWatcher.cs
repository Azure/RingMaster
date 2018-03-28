// <copyright file="DelegateWatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;

    /// <summary>
    /// Class DelegateWatcher.
    /// </summary>
    public class DelegateWatcher : IWatcher
    {
        /// <summary>
        /// The on process
        /// </summary>
        private Action<WatchedEvent> onProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateWatcher"/> class.
        /// </summary>
        /// <param name="onProcess">The on process.</param>
        /// <param name="kind">Kind of the watcher</param>
        /// <exception cref="System.ArgumentNullException">onProcess cannot be null</exception>
        public DelegateWatcher(Action<WatchedEvent> onProcess, WatcherKind kind = WatcherKind.OneUse)
        {
            this.onProcess = onProcess ?? throw new ArgumentNullException("onProcess");
            this.Kind = kind;
        }

        /// <summary>
        /// Gets or sets the unique id of this watcher.
        /// </summary>
        /// <value>The identifier for this watcher.</value>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets a value indicating whether the watcher is for a single use only.
        /// </summary>
        public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

        /// <summary>
        /// Gets or sets the kind of the watcher, if it is for single use and if the data is included on notification
        /// </summary>
        public WatcherKind Kind { get; set; }

        /// <summary>
        /// Processes the specified event.
        /// </summary>
        /// <param name="evt">The event.</param>
        public void Process(WatchedEvent evt)
        {
            this.onProcess(evt);
        }
    }
}