// <copyright file="ObjectTracker.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Class ObjectTracker.
    /// </summary>
    /// <typeparam name="T">The type of the objects to track</typeparam>
    public sealed class ObjectTracker<T> : IDisposable where T : class
    {
        /// <summary>
        /// The _objects by identifier
        /// </summary>
        private readonly Dictionary<ulong, T> objectsById = new Dictionary<ulong, T>();

        /// <summary>
        /// The _ids by object
        /// </summary>
        private readonly DictionaryOfCollection<T, ulong, HashSet<ulong>> idsByObject =
            new DictionaryOfCollection<T, ulong, HashSet<ulong>>();

        /// <summary>
        /// The _rwlock
        /// </summary>
        private ReaderWriterLockSlim rwlock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// The _obj identifier provider
        /// </summary>
        private readonly UIdProvider objIdProvider;

        /// <summary>
        /// The _require unique ids
        /// </summary>
        private readonly bool requireUniqueIds;

        /// <summary>
        /// The _default object ids
        /// </summary>
        private static readonly UIdProvider DefaultObjIds = new UIdProvider(1);

        /// <summary>
        /// The identifier for null
        /// </summary>
        public const ulong IdForNull = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTracker{T}"/> class.
        /// </summary>
        public ObjectTracker()
            : this(DefaultObjIds, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTracker{T}"/> class.
        /// </summary>
        /// <param name="objIdProvider">The object identifier provider.</param>
        /// <param name="requireUniqueIds">if set to <c>true</c> [require unique ids].</param>
        /// <exception cref="System.ArgumentException">IdProvider should not allow for  + IdForNull</exception>
        public ObjectTracker(UIdProvider objIdProvider, bool requireUniqueIds)
        {
            if (objIdProvider == null)
            {
                throw new ArgumentNullException(nameof(objIdProvider));
            }

            this.requireUniqueIds = requireUniqueIds;
            this.objIdProvider = objIdProvider;
            if (!objIdProvider.IsIdInPast((long)IdForNull))
            {
                throw new ArgumentException("IdProvider should not allow for " + IdForNull);
            }
        }

        /// <summary>
        /// Creates the new identifier for object.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        /// <returns>System.UInt64.</returns>
        public ulong CreateNewIdForObject(T watcher)
        {
            return this.GetOrCreateIdForObject(watcher, true);
        }

        /// <summary>
        /// Gets the or create identifier for object.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        /// <returns>System.UInt64.</returns>
        public ulong GetOrCreateIdForObject(T watcher)
        {
            return this.GetOrCreateIdForObject(watcher, false);
        }

        /// <summary>
        /// Gets all.
        /// </summary>
        /// <returns>IEnumerable&lt;T&gt;.</returns>
        public List<T> GetAll()
        {
            this.rwlock.EnterReadLock();
            try
            {
                return new List<T>(this.objectsById.Values);
            }
            finally
            {
                this.rwlock.ExitReadLock();
            }
        }

        /// <summary>
        /// Clears all objects.
        /// </summary>
        public void Clear()
        {
            this.rwlock.EnterWriteLock();
            try
            {
                this.objectsById.Clear();
                this.idsByObject.Clear();
            }
            finally
            {
                this.rwlock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the or create identifier for object.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        /// <param name="alwayscreate">if set to <c>true</c> [alwayscreate].</param>
        /// <returns>System.UInt64.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// CreateIdForObject was called, requireUniqueIds is true and the object is already in the tracker
        /// or
        /// GetOrCreateIdForObject was called, requireUniqueIds is false and the object is already in the tracker
        /// </exception>
        private ulong GetOrCreateIdForObject(T watcher, bool alwayscreate)
        {
            ulong id = 0;

            if (watcher == null)
            {
                return IdForNull;
            }

            this.rwlock.EnterUpgradeableReadLock();
            try
            {
                if (alwayscreate)
                {
                    if (this.requireUniqueIds && this.idsByObject.ContainsAny(watcher))
                    {
                        throw new InvalidOperationException("CreateIdForObject was called, requireUniqueIds is true and the object is already in the tracker");
                    }
                }
                else
                {
                    if (this.idsByObject.ContainsAny(watcher))
                    {
                        if (!this.requireUniqueIds)
                        {
                            throw new InvalidOperationException("GetOrCreateIdForObject was called, requireUniqueIds is false and the object is already in the tracker");
                        }

                        using (IEnumerator<ulong> enume = this.idsByObject.GetValues(watcher).GetEnumerator())
                        {
                            enume.MoveNext();
                            return enume.Current;
                        }
                    }
                }

                do
                {
                    id = this.objIdProvider.NextUniqueId();
                    if (!this.objectsById.ContainsKey(id))
                    {
                        this.rwlock.EnterWriteLock();
                        try
                        {
                            this.objectsById.Add(id, watcher);
                            this.idsByObject.Add(watcher, id);
                        }
                        finally
                        {
                            this.rwlock.ExitWriteLock();
                        }

                        break;
                    }
                }
                while (true);
            }
            finally
            {
                this.rwlock.ExitUpgradeableReadLock();
            }

            return id;
        }

        /// <summary>
        /// Gets the ids for object.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        /// <returns>IEnumerable&lt;System.UInt64&gt;.</returns>
        public IEnumerable<ulong> GetIdsForObject(T watcher)
        {
            if (watcher == null)
            {
                yield return IdForNull;
            }
            else
            {
                this.rwlock.EnterUpgradeableReadLock();
                try
                {
                    foreach (ulong val in this.idsByObject.GetValues(watcher))
                    {
                        yield return val;
                    }
                }
                finally
                {
                    this.rwlock.ExitUpgradeableReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the object for identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="forgetIfFound">if set to <c>true</c> [forget if found].</param>
        /// <returns>T.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public T GetObjectForId(ulong id, bool forgetIfFound = false)
        {
            T obj;

            if (id == IdForNull)
            {
                return null;
            }

            this.rwlock.EnterUpgradeableReadLock();
            try
            {
                bool found = this.objectsById.TryGetValue(id, out obj);

                if (found && forgetIfFound)
                {
                    this.rwlock.EnterWriteLock();
                    try
                    {
                        this.objectsById.Remove(id);
                    }
                    finally
                    {
                        this.rwlock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                this.rwlock.ExitUpgradeableReadLock();
            }

            if (obj == null)
            {
                throw new KeyNotFoundException(string.Empty + id);
            }

            return obj;
        }

        /// <summary>
        /// Forgets the object.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns><c>true</c> if item was present (and hence removed now), <c>false</c> otherwise.</returns>
        public bool ForgetObject(ulong id)
        {
            if (id == IdForNull)
            {
                return true;
            }

            bool result = false;
            this.rwlock.EnterUpgradeableReadLock();
            try
            {
                T obj;
                if (this.objectsById.TryGetValue(id, out obj))
                {
                    this.rwlock.EnterWriteLock();
                    try
                    {
                        this.objectsById.Remove(id);
                        this.idsByObject.Remove(obj, id);
                    }
                    finally
                    {
                        this.rwlock.ExitWriteLock();
                    }

                    result = true;
                }
            }
            finally
            {
                this.rwlock.ExitUpgradeableReadLock();
            }

            return result;
        }

        /// <summary>
        /// Forgets the object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if item was present (and hence removed now), <c>false</c> otherwise.</returns>
        public bool ForgetObject(T obj)
        {
            if (obj == null)
            {
                return true;
            }

            bool result = false;

            this.rwlock.EnterWriteLock();
            try
            {
                foreach (ulong id in this.idsByObject.GetValues(obj))
                {
                    this.objectsById.Remove(id);
                    result = true;
                }

                this.idsByObject.Remove(obj);
            }
            finally
            {
                this.rwlock.ExitWriteLock();
            }

            return result;
        }

        /// <summary>
        /// Sets the identifier for new object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="id">The identifier.</param>
        /// <exception cref="System.ArgumentException">
        /// watcherId must be IdForNull if obj is null
        /// or
        /// object already registered with watcherId:  + id
        /// or
        /// watcherId already in use  + id
        /// </exception>
        public void SetIdForNewObject(T obj, ulong id)
        {
            if (obj == null)
            {
                if (id != IdForNull)
                {
                    throw new ArgumentException("watcherId must be IdForNull if obj is null");
                }

                return;
            }

            this.rwlock.EnterUpgradeableReadLock();
            try
            {
                if (this.idsByObject.Contains(obj, id))
                {
                    // otherwise, we are good to go
                    return;
                }

                if (this.requireUniqueIds && this.idsByObject.ContainsAny(obj))
                {
                    throw new ArgumentException("object already registered with watcherId: " + id);
                }

                if (this.objectsById.ContainsKey(id))
                {
                    throw new ArgumentException("watcherId already in use " + id);
                }

                this.rwlock.EnterWriteLock();
                try
                {
                    this.objectsById.Add(id, obj);
                    this.idsByObject.Add(obj, id);
                }
                finally
                {
                    this.rwlock.ExitWriteLock();
                }
            }
            finally
            {
                this.rwlock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.rwlock?.Dispose();
            this.rwlock = null;
        }
    }
}