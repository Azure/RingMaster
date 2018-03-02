// ***********************************************************************
// Assembly         : RingMasterCommon
// <copyright file="PrefixedClientCache.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class PrefixedClientCache is a simple implementation of a cache for GetData GetChildren and GetACLs
    /// </summary>
    public class PrefixedClientCache : IRingMasterClientCache
    {
        /// <summary>
        /// The caches by prefix
        /// </summary>
        private Dictionary<string, LruCache<string, DataEntry>> cachesByPrefix = new Dictionary<string, LruCache<string, DataEntry>>();

        /// <summary>
        /// Indicates if we want to debug the cache
        /// </summary>
        private bool debugCache = false;
        
        /// <summary>
        /// The maximum capacity per prefix
        /// </summary>
        private int maxCapacityPerPrefix = 10000;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefixedClientCache"/> class.
        /// </summary>
        public PrefixedClientCache()
        {
        }

        /// <summary>
        /// Gets or sets the maximum capacity per prefix.
        /// </summary>
        /// <value>The maximum capacity per prefix.</value>
        public int MaxCapacityPerPrefix
        {
            get
            {
                return this.maxCapacityPerPrefix;
            }

            set
            {
                this.maxCapacityPerPrefix = value;

                lock (this.cachesByPrefix)
                {
                    foreach (LruCache<string, DataEntry> cache in this.cachesByPrefix.Values)
                    {
                        cache.ChangeCapacity(value, false);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the parent of the given path
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>the parent path.</returns>
        public static string GetParent(string path)
        {
            if (path == null)
            {
                return string.Empty;
            }

            int idx = path.LastIndexOf("/");

            if (idx == -1)
            {
                return string.Empty;
            }

            return path.Substring(0, idx);
        }

        /// <summary>
        /// Invalidates the specified path for the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="path">The path.</param>
        public void Invalidate(string prefix, string path)
        {
            LruCache<string, DataEntry> cache = this.GetCacheForPrefix(prefix, false);

            if (cache == null)
            {
                return;
            }

            if (cache.Remove(path))
            {
                if (this.debugCache)
                {
                    System.Console.Write(string.Format("*** INVALIDATED {0}/{1} at {2}", prefix, path, this.GetStack()));
                }
            }
        }

        /// <summary>
        /// Tries to get information.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="path">The path.</param>
        /// <param name="kind">The kind of data to retrieve.</param>
        /// <param name="info">The information retrieved.</param>
        /// <returns><c>true</c> if data has been retrieved, <c>false</c> otherwise.</returns>
        public bool TryGetInfo(string prefix, string path, CachedKind kind, out IRingMasterClientCacheDataEntry info)
        {
            info = null;
            bool result = true;

            try
            {
                LruCache<string, DataEntry> cache = this.GetCacheForPrefix(prefix, false);
                if (cache == null)
                {
                    result = false;
                    return result;
                }

                DataEntry entry;
                if (!cache.TryGetValue(path, out entry))
                {
                    result = false;
                    return result;
                }

                DataEntry copy = new DataEntry();
                info = copy;

                if ((kind & CachedKind.NodeAcls) != CachedKind.None)
                {
                    copy.Acls = entry.Acls;
                }

                if ((kind & CachedKind.NodeData) != CachedKind.None)
                {
                    copy.Data = entry.Data;
                }

                if ((kind & CachedKind.NodeChildren) != CachedKind.None)
                {
                    copy.Children = entry.Children;
                }

                if ((kind & CachedKind.NodeStats) != CachedKind.None)
                {
                    copy.Stat = entry.Stat;
                }

                return result;
            }
            finally
            {
                if (this.debugCache)
                {
                    System.Console.Write(string.Format("*** GETINFO {0}/{1}/{2} --> {3} at {4}", prefix, path, kind, result, this.GetStack()));
                }
            }
        }

        /// <summary>
        /// Sets the information.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="path">The path.</param>
        /// <param name="kind">The kind of info to set.</param>
        /// <param name="info">The information to set.</param>
        public void SetInfo(string prefix, string path, CachedKind kind, IRingMasterClientCacheDataEntry info)
        {
            if (info == null)
            {
                this.Invalidate(prefix, path);
                return;
            }

            LruCache<string, DataEntry> cache = this.GetCacheForPrefix(prefix, true);
            DataEntry entry;
            if (!cache.TryGetValue(path, out entry))
            {
                lock (cache)
                {
                    if (!cache.TryGetValue(path, out entry))
                    {
                        entry = new DataEntry();
                        cache.Add(path, entry);
                    }
                }
            }

            lock (entry)
            {
                if ((kind & CachedKind.NodeAcls) != CachedKind.None)
                {
                    entry.Acls = info.Acls;
                }

                if ((kind & CachedKind.NodeData) != CachedKind.None)
                {
                    entry.Data = info.Data;
                }

                if ((kind & CachedKind.NodeChildren) != CachedKind.None)
                {
                    entry.Children = info.Children;
                }

                if ((kind & CachedKind.NodeStats) != CachedKind.None)
                {
                    entry.Stat = info.Stat;
                }

                if (entry.Stat == null && entry.Children == null && entry.Data == null && entry.Acls == null)
                {
                    cache.Remove(path);
                }
            }

            if (this.debugCache)
            {
                System.Console.Write(string.Format("*** SETINFO {0}/{1}/{2} at {3}", prefix, path, kind, this.GetStack()));
            }
        }

        /// <summary>
        /// Wipes the specified cache for the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        public void Wipe(string prefix)
        {
            lock (this.cachesByPrefix)
            {
                this.cachesByPrefix.Remove(prefix);
            }
        }

        /// <summary>
        /// Notifies the cache about a watcher event.
        /// </summary>
        /// <param name="rc">The result code.</param>
        /// <param name="onChange">The watcher event.</param>
        /// <param name="cachePrefix">The cache prefix to use.</param>
        /// <returns><c>true</c> if the caller needs to restart the watcher, <c>false</c> otherwise.</returns>
        public bool NotifyWatcherEvent(RingMasterException.Code rc, WatchedEvent onChange, string cachePrefix)
        {
            if (onChange == null)
            {
                return false;
            }

            if (onChange.EventType == WatchedEvent.WatchedEventType.WatcherRemoved)
            {
                this.Wipe(cachePrefix);
                return true;
            }

            if (onChange.KeeperState == WatchedEvent.WatchedEventKeeperState.SyncConnected)
            {
                if (onChange.EventType == WatchedEvent.WatchedEventType.NodeCreated ||
                    onChange.EventType == WatchedEvent.WatchedEventType.NodeDeleted)
                {
                    this.Invalidate(cachePrefix, PrefixedClientCache.GetParent(onChange.Path));
                }

                this.Invalidate(cachePrefix, onChange.Path);
            }

            return false;
        }

        /// <summary>
        /// Gets the cache for the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="createIfNonExisting">if set to <c>true</c> [create if non existing].</param>
        /// <returns>LRUCache&lt;System.String, DataEntry&gt;.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "LRU cache is stored in field")]
        private LruCache<string, DataEntry> GetCacheForPrefix(string prefix, bool createIfNonExisting)
        {
            LruCache<string, DataEntry> cache;
            if (this.cachesByPrefix.TryGetValue(prefix, out cache))
            {
                return cache;
            }

            if (!createIfNonExisting)
            {
                return null;
            }

            lock (this.cachesByPrefix)
            {
                if (!this.cachesByPrefix.TryGetValue(prefix, out cache))
                {
                    cache = new LruCache<string, DataEntry>(100);
                    this.cachesByPrefix[prefix] = cache;
                    cache.ChangeCapacity(this.MaxCapacityPerPrefix, false);
                }

                return cache;
            }
        }

        /// <summary>
        /// Gets the stack.
        /// </summary>
        /// <returns>text for the stack</returns>
        private string GetStack()
        {
            string stackstr = (new StackTrace(1, true)).ToString();
            return stackstr;
            /*
            string[] lines = stackstr.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            foreach (string line in lines)
            {
                if (line.Contains("System.Threading"))
                {
                    break;
                }

                sb.AppendLine(line);
            }

            return sb.ToString();
            */
        }

        /// <summary>
        /// Class DataEntry class for the Cache
        /// </summary>
        public class DataEntry : IRingMasterClientCacheDataEntry
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DataEntry"/> class.
            /// </summary>
            public DataEntry()
            {
            }

            /// <summary>
            /// Gets or sets the data.
            /// </summary>
            /// <value>The data.</value>
            public byte[] Data { get; set; }

            /// <summary>
            /// Gets or sets the IStat.
            /// </summary>
            /// <value>The stat.</value>
            public IStat Stat { get; set; }

            /// <summary>
            /// Gets or sets the children.
            /// </summary>
            /// <value>The children.</value>
            public IReadOnlyList<string> Children { get; set; }

            /// <summary>
            /// Gets or sets the ACLs.
            /// </summary>
            /// <value>The ACLs.</value>
            public IReadOnlyList<Acl> Acls { get; set; }
        }
    }
}