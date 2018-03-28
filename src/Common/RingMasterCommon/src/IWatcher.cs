// <copyright file="IWatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    /// <summary>
    /// Interface IWatcher
    /// </summary>
    public interface IWatcher
    {
        /// <summary>
        /// Gets the unique id of this watcher.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// Gets a value indicating whether the watcher is for a single use only.
        /// </summary>
        /// <value><c>true</c> if this is a single-use watcher; otherwise, <c>false</c>.</value>
        bool OneUse { get; }

        /// <summary>
        /// Gets the kind of the watcher, if it is for single use and if the data is included on notification
        /// </summary>
        WatcherKind Kind { get; }

        /// <summary>
        /// Processes the specified event.
        /// </summary>
        /// <param name="evt">The event</param>
        void Process(WatchedEvent evt);
    }
}