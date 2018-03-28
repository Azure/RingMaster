// <copyright file="CompleteNode.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Native;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Class CompleteNode.
    /// Note: childrenNode's key is a string (the name)
    /// </summary>
    public class CompleteNode : Node
    {
        /// <summary>
        /// Mapping from the child name to the persisted data
        /// </summary>
        /// <remarks>
        /// Accessing the children nodes is NOT under a lock and thus may have race condition and unexpected
        /// consequence. Therefore, this object is created in the constructor to at least avoid null reference.
        /// </remarks>
        private IDictionary<string, IPersistedData> childrenMapping = Strategies.CreateChildrenDictionary();

        /// <summary>
        /// Initializes a new instance of the <see cref="CompleteNode" /> class.
        /// </summary>
        /// <param name="pd">The pd.</param>
        public CompleteNode(IPersistedData pd)
            : base(pd)
        {
        }

        /// <summary>
        /// Gets the children mapping in raw
        /// </summary>
        /// <value>the children</value>
        public override IDictionary<string, IPersistedData> ChildrenMapping
        {
            get { return this.childrenMapping; }
        }

        /// <summary>
        /// Gets the children count.
        /// </summary>
        /// <value>The children count.</value>
        public override int ChildrenCount
        {
            get
            {
                return this.childrenMapping.Count;
            }
        }

        /// <summary>
        /// Gets the children nodes.
        /// </summary>
        /// <value>The children nodes.</value>
        public IEnumerable<IPersistedData> ChildrenNodes
        {
            get { return this.childrenMapping.Values; }
        }

        /// <summary>
        /// Gets or sets the watchers.
        /// </summary>
        /// <value>The watchers.</value>
        protected override ICollection<IWatcher> Watchers { get; set; }

        /// <summary>
        /// Creates the node.
        /// </summary>
        /// <param name="pd">The persisted data object</param>
        /// <returns>Node object being created</returns>
        public static Node CreateNode(IPersistedData pd)
        {
            if (pd == null)
            {
                throw new ArgumentNullException(nameof(pd));
            }

            if (pd.Name.Length == 1 && pd.Name[0] == '/')
            {
                return new RootNode(pd);
            }

            if (pd.GetChildrenCount() == 0)
            {
                return new Node(pd);
            }
            else
            {
                return new CompleteNode(pd);
            }
        }

        /// <summary>
        /// Determines whether this instance is empty.
        /// </summary>
        /// <returns><c>true</c> if this instance is empty; otherwise, <c>false</c>.</returns>
        public override bool IsEmpty()
        {
            return this.childrenMapping.Count == 0;
        }

        /// <summary>
        /// Tries the get child.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowWildcards">if true, if 'name' is not a children, it will return a child '*' if it exists</param>
        /// <returns>Node.</returns>
        public override Node TryGetChild(string name, bool allowWildcards = true)
        {
            IPersistedData pd;
            if (this.childrenMapping.TryGetValue(name, out pd))
            {
                return pd.Node;
            }

            // if allowed, see if the node has the wildcard child
            if (allowWildcards)
            {
                if (this.childrenMapping.TryGetValue("*", out pd))
                {
                    return pd.Node;
                }

                if (this.childrenMapping.TryGetValue("**", out pd))
                {
                    return pd.Node;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds children to this node
        /// </summary>
        /// <param name="children">List of children</param>
        /// <remarks>In use in codex loading - do not remove</remarks>
        public override void AddChildren(IList<IPersistedData> children)
        {
            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.AddChildren(children);
                return;
            }

            Strategies.MaybeUpscaleDictionary(ref this.childrenMapping, newCount: this.ChildrenCount + children.Count);

            foreach (IPersistedData p in children)
            {
                if (p == null || p.Name == null)
                {
                    throw new ArgumentException("Children being added must provide a persisted data name", nameof(children));
                }

                this.childrenMapping.Add(p.Name, p);

                if (!p.IsEphemeral)
                {
                    this.Persisted.AddChild(p);
                }
            }
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="n">The n.</param>
        public override void AddChild(Node n)
        {
            if (n == null)
            {
                throw new ArgumentNullException("n");
            }

            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.AddChild(n);
                return;
            }

            this.childrenMapping.Add(n.Name, n.Persisted);
            Strategies.MaybeUpscaleDictionary(ref this.childrenMapping, newCount: this.ChildrenCount + 1);

            if (!n.Persisted.IsEphemeral)
            {
                this.Persisted.AddChild(n.Persisted);
            }
            else
            {
                n.Persisted.Parent = this.Persisted;
            }
        }

        /// <summary>
        /// Removes the child.
        /// </summary>
        /// <param name="name">The name.</param>
        public override void RemoveChild(string name)
        {
            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.RemoveChild(name);
                return;
            }

            IPersistedData pd;
            bool found = this.childrenMapping.TryGetValue(name, out pd);
            RmAssert.IsTrue(found);
            this.childrenMapping.Remove(name);
            Strategies.MaybeDownscaleDictionary(ref this.childrenMapping);

            if (pd != null && !pd.IsEphemeral)
            {
                this.Persisted.RemoveChild(pd);
            }

            this.RevisitNodeType();
        }

        /// <summary>
        /// Get the list of watchers in string
        /// </summary>
        /// <returns>Watch string array</returns>
        public override string[] GetWatcherList()
        {
            lock (this.Persisted)
            {
                if (this.Persisted.Node != this)
                {
                    return this.Persisted.Node.GetWatcherList();
                }

                List<string> result = new List<string>();

                if (this.Watchers == null)
                {
                    return new string[0];
                }

                foreach (IWatcher w in this.Watchers)
                {
                    result.Add(w.ToString());
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Adds the watcher.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        /// <param name="context">context string?</param>
        public override void AddWatcher(IWatcher watcher, string context = null)
        {
            lock (this.Persisted)
            {
                if (this.Persisted.Node != this)
                {
                    this.Persisted.Node.AddWatcher(watcher, context);
                    return;
                }

                MarshallerChannel.ProxyWatcher pxy = watcher as MarshallerChannel.ProxyWatcher;
                if (pxy != null && context != null)
                {
                    pxy.SetToString(context);
                }

                if (this.Watchers == null)
                {
                    this.Watchers = new LinkedList<IWatcher>();
                }

                this.Watchers.Add(watcher);
            }
        }

        /// <summary>
        /// Removes the watcher.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        public override void RemoveWatcher(IWatcher watcher)
        {
            lock (this.Persisted)
            {
                if (this.Persisted.Node != this)
                {
                    this.Persisted.Node.RemoveWatcher(watcher);
                    return;
                }

                if (this.Watchers != null)
                {
                    this.Watchers.Remove(watcher);

                    if (this.Watchers.Count == 0)
                    {
                        this.Watchers = null;
                    }
                }

                this.RevisitNodeType();
            }
        }

        /// <summary>
        /// Resets the watchers.
        /// </summary>
        /// <param name="newColl">The new coll.</param>
        public override void ResetWatchers(ICollection<IWatcher> newColl)
        {
            if (this.Persisted.Node != this)
            {
                this.Persisted.Node.ResetWatchers(newColl);
                return;
            }

            // note: no watcher lock needed here as this method should only be executed under write lock
            this.Watchers = newColl;

            this.RevisitNodeType();
        }

        /// <summary>
        /// Ensures the complete stat.
        /// </summary>
        /// <param name="persisted">The persisted.</param>
        /// <returns>IStat.</returns>
        internal static IMutableStat EnsureCompleteStat(IPersistedData persisted)
        {
            if (persisted.Stat is FirstStat)
            {
                return new MutableStat(persisted.Stat);
            }

            return persisted.Stat;
        }

        /// <summary>
        /// Revisits the type of the node based on its need for children and watchers.
        /// </summary>
        /// <param name="makeComplete">if set to <c>true</c> the caller demands a 'complete' node.</param>
        /// <returns>new Node to use</returns>
        protected override Node RevisitNodeType(bool makeComplete = false)
        {
            // complete nodes may be demoted to plain nodes
            if (!makeComplete && this.childrenMapping.Count == 0 && this.Watchers == null)
            {
                // and the following line will link _persisted properly
                return new Node(this.Persisted);
            }

            return this.Persisted.Node;
        }

        /// <summary>
        /// Gets the children in sorted order.
        /// </summary>
        /// <param name="startingChildName">Starting child name to get the children</param>
        /// <returns>List of names of children of this node in sorted order</returns>
        protected override IEnumerable<string> GetSortedChildren(string startingChildName)
        {
            AtomicDictionaryFacade<string, IPersistedData> atomicDictionaryFacade = this.childrenMapping as AtomicDictionaryFacade<string, IPersistedData>;
            SortedNameValueDictionary<IPersistedData> sortedDictionary = atomicDictionaryFacade?.UnderlyingDictionary as SortedNameValueDictionary<IPersistedData>;
            if (sortedDictionary == null)
            {
                return GetSortedChildrenWithCondition(this.childrenMapping.Keys, startingChildName);
            }

            return sortedDictionary.GetKeysGreaterThan(startingChildName);
        }

        /// <summary>
        /// Gets the sorted list of children names starting from the given name
        /// </summary>
        /// <param name="names">Collection of children names</param>
        /// <param name="startingChildName">starting child name</param>
        /// <returns>Collection of children names starting from the given name</returns>
        private static IEnumerable<string> GetSortedChildrenWithCondition(ICollection<string> names, string startingChildName)
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            List<string> children;
            if (string.IsNullOrEmpty(startingChildName))
            {
                children = new List<string>(names);
            }
            else
            {
                children = new List<string>(names.Count);

                // Restrict candidates to only names that are greater than the given starting child name.
                foreach (string candidateChild in names)
                {
                    if (string.CompareOrdinal(candidateChild, startingChildName) > 0)
                    {
                        children.Add(candidateChild);
                    }
                }
            }

            children.Sort(StringComparer.Ordinal);

            return children;
        }
    }
}
