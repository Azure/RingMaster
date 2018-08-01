// <copyright file="Node.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Native;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using IGetDataOptionArgument = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.IGetDataOptionArgument;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Class Node.
    /// </summary>
    public class Node
    {
        private static IDictionary<string, IPersistedData> emptyDictionary = new Dictionary<string, IPersistedData>();

        /// <summary>
        /// the lock strategy to follow
        /// </summary>
        private static LockStrategy lockStrategy = new LockStrategy(GetStrategyFromConfig());

        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public Node(IPersistedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Node != null && data.Node != this)
            {
                data.Node.Detach();
            }

            this.Persisted = data;
            data.Node = this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        protected Node()
        {
        }

        /// <summary>
        /// Behavior of wildcard when getting the node object
        /// </summary>
        [Flags]
        internal enum WildCardBehavior
        {
            /// <summary>
            /// Not allowed
            /// </summary>
            NotAllowed = 0x0,

            /// <summary>
            /// Only in the leaf node
            /// </summary>
            AllowInLeaf = 0x1,

            /// <summary>
            /// Allowed in the branch
            /// </summary>
            AllowInBranch = 0x2,

            /// <summary>
            /// Allowed anywhere in the path
            /// </summary>
            AllowAnywhere = AllowInLeaf | AllowInBranch,
        }

        /// <summary>
        /// Lock strategy option
        /// </summary>
        [Flags]
        internal enum LockStrategyOption
        {
            /// <summary>
            /// Use the multi-level reader-writer lock pool
            /// </summary>
            MultiLevelRWLocks = 1,

            /// <summary>
            /// Use a single reader-writer lock for the entire tree
            /// </summary>
            SingleRWLock = 2,

            /// <summary>
            /// Reader does not require a lock
            /// </summary>
            ReaderGoUnlocked = 4,

            /// <summary>
            /// No lock
            /// </summary>
            LocksByBackend = 8,
        }

        /// <summary>
        /// Gets or sets maximum time we can spend in acquiring a rw lock
        /// </summary>
        public static TimeSpan MaxAcquireRWLockTime { get; set; }

        /// <summary>
        /// Gets or sets threshold of number of child nodes for a node below which a dictionary will not be used.
        /// </summary>
        public static int MinDictionaryThreshold { get; set; } = 16;

        /// <summary>
        /// Gets or sets threshold of number of child nodes for a node above which a dictionary will be used.
        /// </summary>
        public static int MaxDictionaryThreshold { get; set; } = 128;

        /// <summary>
        /// Gets or sets threshold of number of child nodes for a node below which a sorted dictionary will not be used.
        /// </summary>
        public static int MinSortedDictionaryThreshold { get; set; } = 40000;

        /// <summary>
        /// Gets or sets threshold of number of child nodes for a node above which a sorted dictionary will be used.
        /// </summary>
        public static int MaxSortedDictionaryThreshold { get; set; } = 50000;

        /// <summary>
        /// Gets or sets the persisted data
        /// </summary>
        public IPersistedData Persisted { get; set; }

        /// <summary>
        /// Gets the global unique identifier during this execution.
        /// </summary>
        /// <value>The global unique identifier.</value>
        public string GlobalUniqueId
        {
            get
            {
                return string.Format("{0}-{1}", this.Persisted.GetType().GetHashCode(), this.Persisted.Id);
            }
        }

        /// <summary>
        /// Gets the children mapping in raw
        /// </summary>
        /// <value>the children</value>
        public virtual IDictionary<string, IPersistedData> ChildrenMapping
        {
            get { return emptyDictionary; }
        }

        /// <summary>
        /// Gets the children count.
        /// </summary>
        /// <value>The children count.</value>
        public virtual int ChildrenCount
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return this.Persisted.Name;
            }
        }

        /// <summary>
        /// Gets the acl.
        /// </summary>
        /// <value>The acl.</value>
        public IEnumerable<Acl> Acl
        {
            get
            {
                return this.Persisted.Acl;
            }
        }

        /// <summary>
        /// Gets the parent node.
        /// </summary>
        public Node Parent
        {
            get
            {
                IPersistedData persistedParent = this.Persisted.Parent;
                return persistedParent == null ? null : persistedParent.Node;
            }
        }

        /// <summary>
        /// Gets the node stat.
        /// </summary>
        /// <value>The node stat.</value>
        public Stat NodeStat
        {
            get
            {
                // note: we need to clone here, since "stat" is not immutable and therefore can change.
                // No point to lock while clining, since the lock is already acquired
                // cloning is needed since the lock will be released before we serialize the reference.
                return new Stat(this.Persisted.Stat);
            }
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>The data.</value>
        public byte[] Data
        {
            get
            {
                // note: no need to cloning, since the byte array is immutable, and we return here the reference
                return this.Persisted.Data;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public virtual bool IsRoot
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the watchers.
        /// </summary>
        /// <value>The watchers.</value>
        /// <exception cref="System.InvalidOperationException">Set operation not allowed</exception>
        protected virtual ICollection<IWatcher> Watchers
        {
            get
            {
                return null;
            }

            set
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// builds the string for the path to this node
        /// </summary>
        /// <param name="d">Persisted data object to build the path</param>
        /// <returns>Path to the node</returns>
        public static string BuildPath(IPersistedData d)
        {
            if (d == null)
            {
                throw new ArgumentNullException("d");
            }

            // TODO: this generates too many strings!
            string path = d.Name;
            d = d.Parent;
            while (d != null)
            {
                if (d.Parent == null)
                {
                    path = d.Name + path;
                    break;
                }

                path = d.Name + "/" + path;
                d = d.Parent;
            }

            return path;
        }

        /// <summary>
        /// Determines whether this instance is empty.
        /// </summary>
        /// <returns><c>true</c> if this instance is empty; otherwise, <c>false</c>.</returns>
        public virtual bool IsEmpty()
        {
            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)(this.Persisted.Id & 0x7fffffff);
        }

        /// <summary>
        /// Sets the acl.
        /// </summary>
        /// <param name="acl">The acl.</param>
        public void SetAcl(IReadOnlyList<Acl> acl)
        {
            this.Persisted.Acl = acl;
        }

        /// <summary>
        /// Gets the path to this node
        /// </summary>
        /// <returns>Path to this node</returns>
        public string BuildPath()
        {
            return BuildPath(this.Persisted);
        }

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="data">The data.</param>
        public void SetData(byte[] data)
        {
            this.Persisted.Data = data;
            this.Persisted.Stat.DataLength = this.Persisted.Data == null ? 0 : this.Persisted.Data.Length;
        }

        /// <summary>
        /// Gets the list of watchers on the node
        /// </summary>
        /// <returns>string array of watchers</returns>
        public virtual string[] GetWatcherList()
        {
            if (this.Persisted.Node != this)
            {
                return this.Persisted.Node.GetWatcherList();
            }

            return new string[0];
        }

        /// <summary>
        /// Adds the watcher.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        /// <param name="context">Not in use</param>
        public virtual void AddWatcher(IWatcher watcher, string context = null)
        {
            lock (this.Persisted)
            {
                if (this.Persisted.Node != this)
                {
                    this.Persisted.Node.AddWatcher(watcher);
                    return;
                }

                CompleteNode cn = (CompleteNode)this.RevisitNodeType(makeComplete: true);
                cn.AddWatcher(watcher);
            }
        }

        /// <summary>
        /// Removes the watcher.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        public virtual void RemoveWatcher(IWatcher watcher)
        {
            lock (this.Persisted)
            {
                if (this.Persisted.Node == this)
                {
                    return;
                }

                this.Persisted.Node.RemoveWatcher(watcher);
            }
        }

        /// <summary>
        /// Resets the watchers.
        /// </summary>
        /// <param name="newColl">The new coll.</param>
        public virtual void ResetWatchers(ICollection<IWatcher> newColl)
        {
            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.ResetWatchers(newColl);
                return;
            }

            if (newColl != null)
            {
                CompleteNode cn = (CompleteNode)this.RevisitNodeType(makeComplete: true);
                cn.ResetWatchers(newColl);
            }
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="n">The node object</param>
        public virtual void AddChild(Node n)
        {
            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.AddChild(n);
                return;
            }

            CompleteNode cn = (CompleteNode)this.RevisitNodeType(makeComplete: true);

            cn.AddChild(n);
        }

        /// <summary>
        /// Removes the child.
        /// </summary>
        /// <param name="name">The name.</param>
        public virtual void RemoveChild(string name)
        {
            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.RemoveChild(name);
                return;
            }

            // nothing to be done here.
        }

        /// <summary>
        /// Tries the get child.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowWildcards">if true, if the name doesn't exist it will look for a '*' child</param>
        /// <returns>Node.</returns>
        public virtual Node TryGetChild(string name, bool allowWildcards = true)
        {
            return null;
        }

        /// <summary>
        /// Completes the change.
        /// </summary>
        /// <param name="chg">The CHG.</param>
        /// <param name="path">The path.</param>
        /// <param name="locklist">The locklist this triggers will be fired on.</param>
        public void ScheduleTriggerWatchers(ChangeKind chg, string path, ILockListTransaction locklist)
        {
            WatchedEvent.WatchedEventType evt = WatchedEvent.WatchedEventType.None;
            byte[] data = null;
            IStat stat = null;

            switch (chg)
            {
                case ChangeKind.NodeCreated:
                    evt = WatchedEvent.WatchedEventType.NodeCreated;

                    // newly created node doesn't have child, this is what we have
                    data = this.Data;
                    stat = this.NodeStat;
                    break;

                case ChangeKind.ChildrenAdded:
                case ChangeKind.ChildrenRemoved:
                    evt = WatchedEvent.WatchedEventType.NodeChildrenChanged;
                    stat = this.NodeStat;
                    break;

                case ChangeKind.DataChanged:
                    evt = WatchedEvent.WatchedEventType.NodeDataChanged;
                    data = this.Data;
                    stat = this.NodeStat;
                    break;

                case ChangeKind.AclChanged:
                    evt = WatchedEvent.WatchedEventType.None;
                    break;

                case ChangeKind.NodeDeleted:
                    evt = WatchedEvent.WatchedEventType.NodeDeleted;
                    break;

                case ChangeKind.None:
                    return;
            }

            if (evt == WatchedEvent.WatchedEventType.None)
            {
                return;
            }

            // note: no watcher lock needed here as this method should only be executed under write lock
            ICollection<IWatcher> watchers = this.Watchers;

            if (watchers == null && !ClientSession.AnyBulkWatcher())
            {
                return;
            }

            var evtWithData = new WatchedEvent(evt, WatchedEvent.WatchedEventKeeperState.SyncConnected, path, data, stat);
            var evtNoData = data == null && stat == null
                ? evtWithData
                : new WatchedEvent(evt, WatchedEvent.WatchedEventKeeperState.SyncConnected, path, null, null);

            if (watchers != null)
            {
                List<IWatcher> newColl = null;

                foreach (IWatcher watcher in watchers)
                {
                    if (!watcher.Kind.HasFlag(WatcherKind.OneUse))
                    {
                        if (newColl == null)
                        {
                            newColl = new List<IWatcher>();
                        }

                        newColl.Add(watcher);
                    }
                }

                this.ResetWatchers(newColl);
            }

            Action actAbort = new Action(() =>
            {
                this.ResetWatchers(watchers);
            });

            Action act = new Action(() =>
            {
                var watchersOnPath = ClientSession.GetBulkWatchers(path);
                if (watchers != null)
                {
                    watchersOnPath = watchersOnPath.Concat(watchers);
                }

                foreach (IWatcher watcher in watchersOnPath)
                {
                    if (watcher.Kind.HasFlag(WatcherKind.IncludeData))
                    {
                        watcher.Process(evtWithData);
                    }
                    else
                    {
                        watcher.Process(evtNoData);
                    }
                }
            });

            if (locklist == null)
            {
                act();
            }
            else
            {
                locklist.RunOnCommit(act);
                locklist.RunOnAbort(actAbort);
            }
        }

        /// <summary>
        /// Detaches this Node instance from the PersistedData reference
        /// </summary>
        public virtual void Detach()
        {
        }

        /// <summary>
        /// indicates if the node's ACLs allow the given client id to get access
        /// </summary>
        /// <param name="auth">the session auth of the requestor</param>
        /// <param name="perm">requested permisions</param>
        public void AclAllows(ISessionAuth auth, Perm perm)
        {
            if (auth == null)
            {
                throw new ArgumentNullException("auth");
            }

            if (this.Persisted.Acl == null || auth.IsSuperSession)
            {
                return;
            }

            bool anyAcl = false;

            // if there is no ACL (empty list) then we are allowed.
            // But if there is any acl, we must be granted by the acl setup
            foreach (Acl a in this.Acl)
            {
                anyAcl = true;

                if ((a.Perms & (int)perm) != 0)
                {
                    switch (a.Id.Scheme)
                    {
                        case Scheme.World: // all is good by the Acl no need to keep checking
                            return;

                        case Scheme.Authenticated: // if the session has a client Id no need to keep checking
                            if (!string.IsNullOrEmpty(auth.ClientIdentity))
                            {
                                return;
                            }

                            if (!string.IsNullOrEmpty(auth.ClientDigest))
                            {
                                return;
                            }

                            break;

                        case Scheme.Host: // if the client's host name is this one (we can use the cert' CN for this)
                            if (auth.ClientIdentity == a.Id.Identifier)
                            {
                                return;
                            }

                            break;

                        case Scheme.Ip: // if the client's IP is this one
                            if (auth.ClientIP == a.Id.Identifier)
                            {
                                return;
                            }

                            break;

                        case Scheme.Digest: // if the client id matches this one
                            if (auth.ClientDigest == Scheme.Digest + ":" + a.Id.Identifier)
                            {
                                return;
                            }

                            break;
                    }
                }
            }

            if (anyAcl)
            {
                throw new InvalidAclException(this.Name, auth.ToString());
            }
        }

        /// <summary>
        /// Adds the given list of persisted data as children of the nodes
        /// </summary>
        /// <param name="children">List of persisted data to be added</param>
        public virtual void AddChildren(IList<IPersistedData> children)
        {
            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.AddChildren(children);
                return;
            }

            CompleteNode cn = (CompleteNode)this.RevisitNodeType(makeComplete: true);
            cn.AddChildren(children);
        }

        /// <summary>
        /// Gets a boolean indicating if this instance has any watcher.
        /// Note, bulk watchers are not included in this response.
        /// </summary>
        /// <returns>true if there are any watchers in this instance</returns>
        internal bool HasWatchers()
        {
            return this.Watchers != null;
        }

        /// <summary>
        /// returns the tree level of the node.
        /// </summary>
        /// <returns>Level of the node</returns>
        internal int GetLevel()
        {
            int l = 0;
            IPersistedData p = this.Persisted;
            while (p.Parent != null)
            {
                l++;
                p = p.Parent;
            }

            return l;
        }

        /// <summary>
        /// Gets the node.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="lockList">The lock list.</param>
        /// <param name="wildcardBehavior">if AllowInLeaf, if the last element in the path doesnt exist, it will return '*' node if it exists. if AllowInBranch, trasversing the path will replace * or ** accordingly.</param>
        /// <param name="accessNode">The access node.</param>
        /// <param name="accessParent">The access parent.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="faultBackOnParent">Return last existing node in the chain</param>
        /// <param name="getDataArg">GetData argument specified by user</param>
        /// <returns>Node.</returns>
        internal Node GetNode(string path, ILockListTransaction lockList, WildCardBehavior wildcardBehavior, Perm accessNode, Perm accessParent, out Node parent, bool faultBackOnParent = false, IGetDataOptionArgument getDataArg = null)
        {
            int childlevel;
            return this.GetNode(path, lockList, wildcardBehavior, accessNode, accessParent, out parent, out childlevel, faultBackOnParent, getDataArg);
        }

        /// <summary>
        /// Gets the node
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="lockList">The lock list.</param>
        /// <param name="wildcardBehavior">if AllowInLeaf, if the last element in the path doesnt exist, it will return '*' node if it exists. if AllowInBranch, trasversing the path will replace * or ** accordingly.</param>
        /// <param name="accessNode">The access node.</param>
        /// <param name="accessParent">The access parent.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="childlevel">Level of the child node on output</param>
        /// <param name="faultBackOnParent">Return last existing node in the chain</param>
        /// <param name="getDataArg">GetData argument specified by user</param>
        /// <returns>The node object</returns>
        internal Node GetNode(string path, ILockListTransaction lockList, WildCardBehavior wildcardBehavior, Perm accessNode, Perm accessParent, out Node parent, out int childlevel, bool faultBackOnParent = false, IGetDataOptionArgument getDataArg = null)
        {
            parent = null;

            Node n = this.JustGetNode(path, lockList, wildcardBehavior, accessParent, out childlevel, out parent, faultBackOnParent, getDataArg);

            // If we find the node, we need to overwrite the parent with the actual parent node.
            // this is because the faultback options may give us some other node (not the exact path we are passing down).
            if (n != null)
            {
                parent = n.Parent;

                if (accessNode != Perm.NONE)
                {
                    if (accessNode == Perm.READ)
                    {
                        lockList.AddLockRo(n, childlevel);
                    }
                    else
                    {
                        lockList.AddLockRw(n, accessNode, childlevel);
                    }
                }
            }

            return n;
        }

        /// <summary>
        /// Gets the node parent of the given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="lockList">The lock list.</param>
        /// <param name="wildcardBehavior">the behavior of wildcards</param>
        /// <param name="lastChildName">Last name of the child to look up on the parent node to find the given path.</param>
        /// <param name="match">the last node that matches the wildcard</param>
        /// <param name="resultnodelevel">the level of the resulted node</param>
        /// <param name="faultBackOnParent">Return last existing node in the chain. If true last existing parent node with child name will be returned.</param>
        /// <param name="getDataArg">GetData argument specified by user</param>
        /// <returns>Node retrieved</returns>
        internal Node GetPathParent(string path, ILockListTransaction lockList, WildCardBehavior wildcardBehavior, out string lastChildName, out Node match, out int resultnodelevel, bool faultBackOnParent = false, IGetDataOptionArgument getDataArg = null)
        {
            int level = 0;

            Node lastParent = this;
            match = null;

            // the most common case is to be called from Root, so we optimize for it.
            if (this.Persisted.Parent != null)
            {
                level = this.GetLevel();
            }

            resultnodelevel = level;

            lockList.AddLockRo(this, level);

            string[] pathpieces = path.Split('/');
            if (pathpieces[0].Length != 0 || (pathpieces.Length == 2 && pathpieces[0].Length == 0 && pathpieces[1].Length == 0))
            {
                lastChildName = null;
                return lastParent;
            }

            Node doubleMarkNode = null;

            bool allowBranch = (wildcardBehavior & WildCardBehavior.AllowInBranch) == WildCardBehavior.AllowInBranch;

            for (int i = 1; i < pathpieces.Length - 1; i++)
            {
                string childName = pathpieces[i];
                Node child = lastParent.TryGetChild(childName, allowBranch);

                if (wildcardBehavior != WildCardBehavior.NotAllowed && child != null && child.Name.Equals("**"))
                {
                    doubleMarkNode = child;
                }

                if (child == null)
                {
                    if (doubleMarkNode != null)
                    {
                        lastChildName = "**";
                        resultnodelevel = doubleMarkNode.Persisted.Parent.Node.GetLevel();
                        return doubleMarkNode.Persisted.Parent.Node;
                    }

                    // Some nodes in the chain do not exist.
                    // Return last existing parent node and the name of the child that does not exist.
                    if (faultBackOnParent && lastParent != null)
                    {
                        lastChildName = childName;
                        return lastParent;
                    }

                    // otherwise, we couldnt find the node for the desired path element.
                    lastChildName = null;
                    resultnodelevel = -1;
                    return null;
                }

                level++;
                lockList.AddLockRo(child, level);
                resultnodelevel = level;
                lastParent = child;

                if (faultBackOnParent &&
                    getDataArg != null &&
                    getDataArg.Matches(lastParent.Persisted.Data))
                {
                    match = lastParent;
                }
            }

            lastChildName = pathpieces[pathpieces.Length - 1];

            if (doubleMarkNode != null)
            {
                if (lastParent.TryGetChild(lastChildName) == null && ((wildcardBehavior & WildCardBehavior.AllowInBranch) == WildCardBehavior.AllowInBranch))
                {
                    lastChildName = "**";
                    lastParent = doubleMarkNode.Persisted.Parent.Node;
                }
            }

            if (faultBackOnParent &&
                lastParent != null &&
                getDataArg != null &&
                getDataArg.Matches(lastParent.Persisted.Data))
            {
                match = lastParent;
            }

            return lastParent;
        }

        /// <summary>
        /// Determines whether the specified version matches this object's.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns><c>true</c> if the specified version is version; otherwise, <c>false</c>.</returns>
        internal bool IsVersion(long version)
        {
            return version == -1 || this.Persisted.Stat.Version == version;
        }

        /// <summary>
        /// Determines whether the specified version and incarnation id match this object's.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="uniqueIncarnationId">The uniqueIncarnationId.</param>
        /// <param name="kind">if the type of validation for uniqueid.</param>
        /// <returns><c>true</c> if the values match; otherwise, <c>false</c>.</returns>
        internal bool IsVersion(long version, Guid uniqueIncarnationId, RequestDefinitions.RequestCheck.UniqueIncarnationIdType kind)
        {
            if (!this.IsVersion(version))
            {
                return false;
            }

            if (kind == RequestDefinitions.RequestCheck.UniqueIncarnationIdType.None || Guid.Equals(uniqueIncarnationId, Guid.Empty))
            {
                return true;
            }

            Guid g2 = Stat.GetUniqueIncarnationId(this.Persisted.Stat, kind == RequestDefinitions.RequestCheck.UniqueIncarnationIdType.Extended);
            return Guid.Equals(uniqueIncarnationId, g2);
        }

        /// <summary>
        /// Determines whether the specified child version is version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns><c>true</c> if the specified version is version; otherwise, <c>false</c>.</returns>
        internal bool IsChildVersion(long version)
        {
            return version == -1 || this.Persisted.Stat.Cversion == version;
        }

        /// <summary>
        /// Determines whether [is acl version] [the specified version].
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns><c>true</c> if acl is in the given version; otherwise, <c>false</c>.</returns>
        internal bool IsAclVersion(long version)
        {
            return version == -1 || this.Persisted.Stat.Aversion == version;
        }

        /// <summary>
        /// Synchronizes the specified zxid.
        /// </summary>
        /// <param name="timeoutInSeconds">Timeout in second</param>
        /// <exception cref="System.TimeoutException">If operation times out</exception>
        internal void Sync(int timeoutInSeconds = 20)
        {
            // nothing really. cheap active loop. TODO: make it an event
            long targetId = this.Persisted.Stat.Czxid;

            targetId = Math.Max(targetId, this.Persisted.Stat.Mzxid);
            targetId = Math.Max(targetId, this.Persisted.Stat.Pzxid);

            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

            while (this.Persisted.SavedZxid < targetId)
            {
                Thread.Sleep(125);

                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException();
                }
            }
        }

        /// <summary>
        /// Gets the lock object.
        /// </summary>
        /// <param name="level">Level of the node</param>
        /// <param name="writeRequired">true for write lock, false for read lock</param>
        /// <returns>The lock object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ILockObject GetLockObject(int level, bool writeRequired)
        {
            return writeRequired
                ? lockStrategy.FindLockForWrite(level, this)
                : lockStrategy.FindLockForRead(level, this);
        }

        /// <summary>
        /// Acquires the lock rw.
        /// </summary>
        /// <param name="level">Level of the node</param>
        /// <returns><c>true</c> if lock was not already acquired yet (and hence acquired here), <c>false</c> otherwise.</returns>
        internal ILockObject AcquireLockRw(int level)
        {
            var duration = Stopwatch.StartNew();

            ILockObject rwLock = lockStrategy.FindLockForWrite(level, this);

            if (rwLock == null)
            {
                return null;
            }

            bool succeeded = rwLock.AcquireWriterLock(MaxAcquireRWLockTime);

            RingMasterServerInstrumentation.Instance.OnAcquireLock(false, succeeded, level, duration.Elapsed);

            if (!succeeded)
            {
                throw new RetriableOperationException("lock couldnt be promoted to write within " + MaxAcquireRWLockTime.TotalMilliseconds + " ms for " + this.Name);
            }

            return rwLock;
        }

        /// <summary>
        /// Enumerates children that match the given retrieval condition.
        /// </summary>
        /// <param name="retrievalCondition">
        /// Condition for the names of the children to retrieve
        /// valid interval definitions:
        /// <c>
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </c>
        /// </param>
        /// <param name="maxChildren">Maximum number of children to retrieve</param>
        /// <returns>List of children names</returns>
        internal IEnumerable<string> RetrieveChildren(string retrievalCondition = null, int maxChildren = int.MaxValue)
        {
            if (string.IsNullOrEmpty(retrievalCondition))
            {
                return this.GetSortedChildren(string.Empty);
            }

            if (retrievalCondition.StartsWith(">:"))
            {
                // Retrieval condition must be of the form: ">:[Top]:[ChildName]". Child name may have colon, such
                // as IPv6 addresses. This assumes after the second colon, every char belongs to child name.
                int index = retrievalCondition.IndexOf(':', 2);
                int top = 0;

                if (index < 0 ||
                    !int.TryParse(retrievalCondition.Substring(2, index - 2), out top) ||
                    top < 0 ||
                    top > maxChildren)
                {
                    RingMasterEventSource.Log.RequestGetChildrenInvalidRetrievalCondition(retrievalCondition);
                    throw new ArgumentException("RetrievalCondition:Top");
                }

                string startingChildName = retrievalCondition.Substring(index + 1);

                return this.GetSortedChildren(startingChildName).Take(top);
            }

            throw new ArgumentException("RetrievalCondition");
        }

        /// <summary>
        /// Revisits the type of the node based on its need for children and watchers.
        /// </summary>
        /// <param name="makeComplete">if set to <c>true</c> the caller demands a 'complete' node.</param>
        /// <returns>new Node to use</returns>
        protected virtual Node RevisitNodeType(bool makeComplete = false)
        {
            // plain nodes may only be promoted to complete nodes
            if (makeComplete)
            {
                // and the following line will link _persisted properly
                return new CompleteNode(this.Persisted);
            }

            return this.Persisted.Node;
        }

        /// <summary>
        /// Gets the children in sorted order.
        /// </summary>
        /// <param name="startingChildName">Not in use</param>
        /// <returns>List of names of children of this node in sorted order</returns>
        protected virtual IEnumerable<string> GetSortedChildren(string startingChildName)
        {
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// finds the option from config
        /// </summary>
        /// <returns>Lock strategy</returns>
        private static LockStrategyOption GetStrategyFromConfig()
        {
            LockStrategyOption option = LockStrategyOption.MultiLevelRWLocks;

            string str = RingMasterBackendCore.GetSetting("RingMaster.LockStrategy");
            if (!Enum.TryParse<LockStrategyOption>(str, out option))
            {
                Trace.TraceInformation("Could not parse Lock Strategy from config: {0}", str);
                option = LockStrategyOption.MultiLevelRWLocks;
            }

            Trace.TraceInformation("Using Lock Strategy from config: {0}", option);
            return option;
        }

        /// <summary>
        /// Gets the node for a given path, with search options
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="lockList">The lock list.</param>
        /// <param name="wildcardBehavior">The wildcard behavior.</param>
        /// <param name="accessParent">The access for the parent.</param>
        /// <param name="childlevel">The childlevel.</param>
        /// <param name="defaultParent">The default parent in case we don't find the exact node.</param>
        /// <param name="faultBackOnParent">if set to <c>true</c> [fault back on parent].</param>
        /// <param name="getDataArg">The get data argument.</param>
        /// <returns>Node.</returns>
        /// <exception cref="System.ArgumentException">faultBackOnParent option not allowed for write access;faultBackOnParent</exception>
        private Node JustGetNode(string path, ILockListTransaction lockList, WildCardBehavior wildcardBehavior, Perm accessParent, out int childlevel, out Node defaultParent, bool faultBackOnParent = false, IGetDataOptionArgument getDataArg = null)
        {
            Node lastMatch;
            Node nodeResponse = null;
            string lastChildName;
            int level = 0;
            defaultParent = null;

            // the most common case is to be called from Root, so we optimize for it.
            if (this.Persisted.Parent != null)
            {
                level = this.GetLevel();
            }

            if (path == "/")
            {
                childlevel = level;
                return this;
            }

            int parentlevel;
            defaultParent = this.GetPathParent(path, lockList, wildcardBehavior, out lastChildName, out lastMatch, out parentlevel, faultBackOnParent, getDataArg);

            if (faultBackOnParent && getDataArg != null)
            {
                // This support not required right now. Can be fairly easily added later if needed
                if (accessParent > Perm.READ)
                {
                    throw new ArgumentException("faultBackOnParent option not allowed for write access", "faultBackOnParent");
                }

                nodeResponse = lastMatch;
            }

            if (defaultParent == null || lastChildName == null)
            {
                if (nodeResponse == null)
                {
                    childlevel = -1;
                    return null;
                }

                childlevel = nodeResponse.GetLevel();
                return nodeResponse;
            }

            // Note: Need to upgrade parent lock from read to write before obtaining child lock
            // to ensure locks are always obtained in hierarchical order. Also note we need to
            // obtain this write lock before attempting TryGetChild to ensure the child is not
            // removed while we are waiting to be upgraded to write lock
            if (accessParent > Perm.READ)
            {
                lockList.AddLockRw(defaultParent, accessParent, parentlevel);
            }

            Node child = defaultParent.TryGetChild(lastChildName, (wildcardBehavior & WildCardBehavior.AllowInLeaf) == WildCardBehavior.AllowInLeaf);

            if (child == null)
            {
                if (nodeResponse == null)
                {
                    childlevel = -1;
                    return null;
                }

                childlevel = nodeResponse.GetLevel();
                return nodeResponse;
            }

            childlevel = parentlevel + 1;

            if (faultBackOnParent && getDataArg != null)
            {
                // Need to lock child before accessing child.Persisted.Data
                lockList.AddLockRo(child, childlevel);

                if (!getDataArg.Matches(child.Persisted.Data))
                {
                    if (lastMatch != null)
                    {
                        child = lastMatch;
                        childlevel = child.GetLevel();
                    }
                    else
                    {
                        child = null;
                        childlevel = -1;
                    }
                }
            }

            return child;
        }

        /// <summary>
        /// Class Strategies. It encapsulates the strategies for creating collections to hold children.
        /// </summary>
        internal static class Strategies
        {
            /// <summary>
            /// Creates the children dictionary for the Nodes.
            /// </summary>
            /// <returns>IDictionary&lt;System.String, IPersistedData&gt;.</returns>
            public static IDictionary<string, IPersistedData> CreateChildrenDictionary()
            {
                return new SortedArrayList<string, IPersistedData>(MinDictionaryThreshold);

                // return new SortedList<string, IPersistedData>(128);
                // return new Dictionary<string, IPersistedData>();
                // return new SkipList<string, IPersistedData>();
            }

            /// <summary>
            /// turns a regular dictionary into a sorted dictionary if it is too big to sort faster than 500ms
            /// </summary>
            /// <param name="childrenNodes">the dictionary</param>
            /// <param name="newCount">New count of items in the dictionary</param>
            internal static void MaybeUpscaleDictionary(ref IDictionary<string, IPersistedData> childrenNodes, int newCount)
            {
                if (newCount > MaxSortedDictionaryThreshold)
                {
                    if (!(childrenNodes is AtomicDictionaryFacade<string, IPersistedData>))
                    {
                        childrenNodes = new AtomicDictionaryFacade<string, IPersistedData>(new SortedNameValueDictionary<IPersistedData>(childrenNodes));
                    }
                }
                else if (newCount > MaxDictionaryThreshold)
                {
                    if (!(childrenNodes is Dictionary<string, IPersistedData>))
                    {
                        childrenNodes = new Dictionary<string, IPersistedData>(childrenNodes);
                    }
                }
                else if (childrenNodes == null)
                {
                    childrenNodes = CreateChildrenDictionary();
                }
            }

            /// <summary>
            /// turns a sorted dictionary into a regular dictionary if it is too small to keep sorted
            /// </summary>
            /// <param name="childrenNodes">the dictionary</param>
            internal static void MaybeDownscaleDictionary(ref IDictionary<string, IPersistedData> childrenNodes)
            {
                if (childrenNodes == null)
                {
                    childrenNodes = CreateChildrenDictionary();
                }
                else if (childrenNodes.Count < MinDictionaryThreshold)
                {
                    if (childrenNodes is Dictionary<string, IPersistedData> || childrenNodes is AtomicDictionaryFacade<string, IPersistedData>)
                    {
                        childrenNodes = CreateChildrenDictionary(childrenNodes);
                    }
                }
                else if (childrenNodes.Count < MinSortedDictionaryThreshold)
                {
                    if (childrenNodes is AtomicDictionaryFacade<string, IPersistedData>)
                    {
                        childrenNodes = new Dictionary<string, IPersistedData>(childrenNodes);
                    }
                }
            }

            /// <summary>
            /// Creates the children dictionary with an initial content.
            /// </summary>
            /// <param name="entries">The entries. It is assumed to fit in the capacity of the small children dictionary</param>
            /// <returns>the populated dictionary</returns>
            private static IDictionary<string, IPersistedData> CreateChildrenDictionary(IDictionary<string, IPersistedData> entries)
            {
                IDictionary<string, IPersistedData> dict = CreateChildrenDictionary();
                foreach (KeyValuePair<string, IPersistedData> d in entries)
                {
                    dict.Add(d.Key, d.Value);
                }

                return dict;
            }
        }

        /// <summary>
        /// Lock strategy
        /// </summary>
        internal class LockStrategy
        {
            private MultiLevelLockPool readerWriterLockPool;
            private ILockObject lockObject;
            private bool readsFree;

            /// <summary>
            /// Initializes a new instance of the <see cref="LockStrategy"/> class.
            /// </summary>
            /// <param name="option">lock strategy option</param>
            internal LockStrategy(LockStrategyOption option)
            {
                this.readsFree = false;

                if ((option & LockStrategyOption.ReaderGoUnlocked) == LockStrategyOption.ReaderGoUnlocked)
                {
                    this.readsFree = true;
                    option -= LockStrategyOption.ReaderGoUnlocked;
                }

                switch (option)
                {
                    case LockStrategyOption.MultiLevelRWLocks:
                        {
                            var lockSizes = GetLockSizesPerLevelFromConfig();
                            LockLevelCount = lockSizes.Length;
                            this.readerWriterLockPool = new MultiLevelLockPool(lockSizes, true);
                            this.lockObject = null;
                            return;
                        }

                    case LockStrategyOption.SingleRWLock:
                        {
                            this.lockObject = new LockObject();
                            this.readerWriterLockPool = null;
                            return;
                        }

                    case LockStrategyOption.LocksByBackend:
                        {
                            this.lockObject = null;
                            this.readerWriterLockPool = null;
                            return;
                        }

                    default:
                        throw new InvalidOperationException("cannot establish a suitable lock strategy");
                }
            }

            /// <summary>
            /// Gets the number of levels of the lock pool
            /// </summary>
            internal static int LockLevelCount { get; private set; }

            /// <summary>
            /// Finds a reader-writer lock object
            /// </summary>
            /// <param name="level">Level of the node object</param>
            /// <param name="node">Node object</param>
            /// <returns>Lock object</returns>
            internal ILockObject FindLockForWrite(int level, Node node)
            {
                if (this.readerWriterLockPool != null)
                {
                    return this.readerWriterLockPool.GetPoolElementFor(level, node);
                }
                else
                {
                    return this.lockObject;
                }
            }

            /// <summary>
            /// Finds a reader lock object
            /// </summary>
            /// <param name="level">Level of the node object</param>
            /// <param name="node">Node object</param>
            /// <returns>Lock object</returns>
            internal ILockObject FindLockForRead(int level, Node node)
            {
                if (this.readsFree)
                {
                    return null;
                }

                if (this.readerWriterLockPool != null)
                {
                    return this.readerWriterLockPool.GetPoolElementFor(level, node);
                }
                else
                {
                    return this.lockObject;
                }
            }

            private static int[] GetLockSizesPerLevelFromConfig()
            {
                string str = RingMasterBackendCore.GetSetting("RingMaster.LockSizesPerLevel");
                try
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        return str.Split(',').Select(int.Parse).ToArray();
                    }
                }
                catch (Exception)
                {
                    Trace.TraceInformation("Could not parse RingMaster.LockSizesPerLevel from config: {0}", str);
                }

                return new int[] { 1, 50, 2500, 10000, 100000, 500000 };
            }
        }
    }
}
