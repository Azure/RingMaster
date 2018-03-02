// ***********************************************************************
// Assembly         : RingMasterCommon
// <copyright file="IRingMasterClientCache.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface IRingMasterClientCache
    /// </summary>
    public interface IRingMasterClientCache
    {
        /// <summary>
        /// Gets or sets the maximum capacity per prefix.
        /// </summary>
        /// <value>The maximum capacity per prefix.</value>
        int MaxCapacityPerPrefix { get; set; }

        /// <summary>
        /// Invalidates the specified path for the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="path">The path.</param>
        void Invalidate(string prefix, string path);

        /// <summary>
        /// Tries to get information.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="path">The path.</param>
        /// <param name="kind">The kind of data to retrieve.</param>
        /// <param name="info">The information retrieved.</param>
        /// <returns><c>true</c> if data has been retrieved, <c>false</c> otherwise.</returns>
        bool TryGetInfo(string prefix, string path, CachedKind kind, out IRingMasterClientCacheDataEntry info);

        /// <summary>
        /// Sets the information.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="path">The path.</param>
        /// <param name="kind">The kind of info to set.</param>
        /// <param name="info">The information to set.</param>
        void SetInfo(string prefix, string path, CachedKind kind, IRingMasterClientCacheDataEntry info);

        /// <summary>
        /// Wipes the specified cache for the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        void Wipe(string prefix);

        /// <summary>
        /// Notifies the cache about a watcher event.
        /// </summary>
        /// <param name="rc">The result code.</param>
        /// <param name="onChange">The watcher event.</param>
        /// <param name="cachePrefix">The cache prefix to use.</param>
        /// <returns><c>true</c> if the caller needs to restart the watcher, <c>false</c> otherwise.</returns>
        bool NotifyWatcherEvent(RingMasterException.Code rc, WatchedEvent onChange, string cachePrefix);
    }

    /// <summary>
    /// Interface IRingMasterClientCacheDataEntry represents an entry in the cache
    /// </summary>
    public interface IRingMasterClientCacheDataEntry
    {
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>The data.</value>
        byte[] Data { get; }

        /// <summary>
        /// Gets the IStat.
        /// </summary>
        /// <value>The stat.</value>
        IStat Stat { get; }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        IReadOnlyList<string> Children { get; }

        /// <summary>
        /// Gets the ACLs.
        /// </summary>
        /// <value>The ACLs.</value>
        IReadOnlyList<Acl> Acls { get; }
    }
}
