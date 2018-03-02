// <copyright file="WatcherCollection.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Collection of Watchers indexed by the path they are watching and then by the session that installed them.
    /// </summary>
    internal sealed class WatcherCollection
    {
        private readonly Dictionary<string, Dictionary<ulong, WatcherRecord>> watchers = new Dictionary<string, Dictionary<ulong, WatcherRecord>>();
        private readonly ReaderWriterLockSlim watchersLock = new ReaderWriterLockSlim();
        private int watcherCount = 0;

        /// <summary>
        /// Gets a value indicating whether any watchers are present in the collection.
        /// </summary>
        public bool IsAnyWatcherPresent
        {
            get
            {
                return this.watcherCount > 0;
            }
        }

        /// <summary>
        /// Check if a watcher that is applicable to the given path is present.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns><c>true</c> if atleast one watcher is applicable to the given path</returns>
        public bool IsWatcherPresent(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // If there is atleast one applicable watcher, return true
            foreach (var watcher in this.EnumerateApplicableWatchers(path))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Enumerate the watchers that must be notified of any changes to the given path.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>A sequence of <see cref="IWatcher"/>s that must be notified</returns>
        public IEnumerable<IWatcher> EnumerateApplicableWatchers(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var applicableWatchers = new List<IWatcher>();
            var timer = Stopwatch.StartNew();
            if (this.watcherCount > 0)
            {
                this.watchersLock.EnterReadLock();
                try
                {
                    foreach (string parentPath in EnumerateParentPaths(path))
                    {
                        Dictionary<ulong, WatcherRecord> existingWatchers;
                        if (this.watchers.TryGetValue(parentPath, out existingWatchers))
                        {
                            foreach (var pair in existingWatchers)
                            {
                                ulong sessionId = pair.Key;
                                WatcherRecord watcher = pair.Value;

                                applicableWatchers.Add(watcher.Watcher);
                                RingMasterEventSource.Log.WatcherCollection_WatcherApplies(path, sessionId, parentPath);
                            }
                        }
                    }
                }
                finally
                {
                    this.watchersLock.ExitReadLock();
                }
            }

            RingMasterEventSource.Log.WatcherCollection_EnumeratedApplicableWatchers(path, applicableWatchers.Count, timer.ElapsedMilliseconds);
            return applicableWatchers;
        }

        /// <summary>
        /// Add a watcher to the path.
        /// </summary>
        /// <param name="sessionId">Id of the session that is installing the watcher</param>
        /// <param name="path">Path to watch</param>
        /// <param name="watcher">Watcher to added</param>
        /// <returns>The number of watchers in the collection after the given watcher was added</returns>
        public int AddWatcher(ulong sessionId, string path, MarshallerChannel.ProxyWatcher watcher)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            WatcherRecord replacedWatcher = null;
            var timer = Stopwatch.StartNew();
            this.watchersLock.EnterWriteLock();
            try
            {
                Dictionary<ulong, WatcherRecord> existingWatchers;
                if (!this.watchers.TryGetValue(path, out existingWatchers))
                {
                    existingWatchers = new Dictionary<ulong, WatcherRecord>();
                    this.watchers.Add(path, existingWatchers);
                }

                if (existingWatchers.TryGetValue(sessionId, out replacedWatcher))
                {
                    RingMasterEventSource.Log.WatcherCollection_RemoveExistingWatcher(sessionId, path);
                    existingWatchers.Remove(sessionId);
                    this.watcherCount--;
                }
                
                if (watcher != null)
                {
                    WatcherRecord newWatcher = new WatcherRecord(watcher);
                    existingWatchers.Add(sessionId, newWatcher);
                    this.watcherCount++;

                    RingMasterEventSource.Log.WatcherCollection_AddWatcher(sessionId, path, this.watcherCount, timer.ElapsedMilliseconds);
                }

                return this.watcherCount;
            }
            finally
            {
                this.watchersLock.ExitWriteLock();

                if (replacedWatcher != null)
                {
                    replacedWatcher.NotifyWatcherRemoved(isSessionTerminating: false);
                }
            }
        }

        /// <summary>
        /// Remove a watcher from the path.
        /// </summary>
        /// <param name="sessionId">Id of the session that is removing the watcher</param>
        /// <param name="path">Path associated with the watcher</param>
        /// <returns>The number of watchers in the collection after the given watcher was removed</returns>
        public int RemoveWatcher(ulong sessionId, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var timer = Stopwatch.StartNew();
            this.watchersLock.EnterWriteLock();
            try
            {
                Dictionary<ulong, WatcherRecord> existingWatchers;
                if (this.watchers.TryGetValue(path, out existingWatchers))
                {
                    WatcherRecord watcher;
                    if (existingWatchers.TryGetValue(sessionId, out watcher))
                    {
                        watcher.NotifyWatcherRemoved(isSessionTerminating: false);
                        existingWatchers.Remove(sessionId);
                        this.watcherCount--;

                        RingMasterEventSource.Log.WatcherCollection_RemoveWatcher(sessionId, path, this.watcherCount, timer.ElapsedMilliseconds);

                        if (existingWatchers.Count == 0)
                        {
                            this.watchers.Remove(path);
                        }
                    }
                }

                return this.watcherCount;
            }
            finally
            {
                this.watchersLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Remove all watchers associated with the given session.
        /// </summary>
        /// <param name="sessionId">Id of the session</param>
        /// <returns>The number of watchers in the collection after the watchers associated with the session were removed</returns>
        public int RemoveAllWatchersForSession(ulong sessionId)
        {
            var timer = Stopwatch.StartNew();
            int removedCount = 0;
            this.watchersLock.EnterWriteLock();
            try
            {
                var cleanupList = new List<string>();
                foreach (var pair in this.watchers)
                {
                    string watchedPath = pair.Key;
                    Dictionary<ulong, WatcherRecord> installedWatchers = pair.Value;

                    WatcherRecord watcher = null;
                    if (installedWatchers.TryGetValue(sessionId, out watcher))
                    {
                        watcher.NotifyWatcherRemoved(isSessionTerminating: true);
                        installedWatchers.Remove(sessionId);
                        this.watcherCount--;
                        removedCount++;

                        if (installedWatchers.Count == 0)
                        {
                            cleanupList.Add(pair.Key);
                        }

                        RingMasterEventSource.Log.WatcherCollection_RemoveWatcherOnSessionTermination(sessionId, watchedPath, this.watcherCount);
                    }
                }

                foreach (var path in cleanupList)
                {
                    RingMasterEventSource.Log.WatcherCollection_CleanupOnSessionTermination(sessionId, path);
                    this.watchers.Remove(path);
                }

                return this.watcherCount;
            }
            finally
            {
                RingMasterEventSource.Log.WatcherCollection_RemovedAllWatchersForSession(sessionId, removedCount, this.watcherCount, timer.ElapsedMilliseconds);
                this.watchersLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Enumerate the list of parent paths of the given path
        /// </summary>
        /// <param name="path">Path whose parents have to be enumerated</param>
        /// <returns>A sequence of paths</returns>
        /// <remarks>
        /// If the given path is /foo/bar/toe
        /// then the following paths will be enumerated.
        /// /
        /// /foo
        /// /foo/bar
        /// /foo/bar/toe
        /// </remarks>
        private static IEnumerable<string> EnumerateParentPaths(string path)
        {
            int index = path.IndexOf('/');

            while (index > -1)
            {
                if (index == 0)
                {
                    yield return "/";
                }
                else
                {
                    yield return path.Substring(0, index);
                }

                index = path.IndexOf('/', index + 1);
            }

            yield return path;
        }

        private sealed class WatcherRecord
        {
            private readonly MarshallerChannel.ProxyWatcher watcher;

            public WatcherRecord(MarshallerChannel.ProxyWatcher watcher)
            {
                this.watcher = watcher;
            }

            public IWatcher Watcher => this.watcher;

            public void NotifyWatcherRemoved(bool isSessionTerminating)
            {
                // Do not bother to send a watcher removed message if the session is terminating.
                this.watcher.ProcessAndAbandon(sendMessage: !isSessionTerminating);
            }
        }
    }
}