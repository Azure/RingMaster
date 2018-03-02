// <copyright file="WatcherCall.cs" company="Microsoft">
//     Copyright ©  2015
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
        /// Gets or sets a value indicating whether this watcher is single use only.
        /// </summary>
        public bool OneUse { get; set; }

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