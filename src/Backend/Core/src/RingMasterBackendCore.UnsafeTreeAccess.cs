// <copyright file="RingMasterBackendCore.UnsafeTreeAccess.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;

    /// <summary>
    /// Backend core - IUnsafeTreeAccess interface implementation
    /// </summary>
    public partial class RingMasterBackendCore : IUnsafeTreeAccess
    {
        /// <summary>
        /// Locks the root.
        /// </summary>
        void IUnsafeTreeAccess.LockRoot()
        {
            if (this.Root != null)
            {
                Trace.TraceInformation("locking root...");

                // lock the root. we are secondary and all replication is being blocked by the replication backend already.
                ((IUnsafeTreeAccess)this).LockRootNoSync();
                Trace.TraceInformation("root sync ready");
            }
        }

        /// <summary>
        /// Locks the root without synchronizing it.
        /// </summary>
        void IUnsafeTreeAccess.LockRootNoSync()
        {
            if (this.Root != null)
            {
                this.rootLock = this.Root.AcquireLockRw(0);
            }
        }

        /// <summary>
        /// Releases the root.
        /// </summary>
        void IUnsafeTreeAccess.ReleaseRoot()
        {
            if (this.Root != null)
            {
                this.rootLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Fixes parent stat and node structures when a child is added
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="child">The child.</param>
        /// <param name="txtime">The tx_time.</param>
        /// <param name="path">The path.</param>
        /// <param name="xid">The tx_id.</param>
        /// <param name="locklist">The locklist for this session.</param>
        void IUnsafeTreeAccess.UnsafeAddChild(IPersistedData parent, IPersistedData child, long txtime, string path, long xid, ILockListTransaction locklist)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (locklist == null && this.IsPathLockedDown(path))
            {
                throw new InvalidAclException(path, "lockdown");
            }

            if (ForceWB || locklist == null)
            {
                this.secondarypreprocessor.AppendAddChild(parent.Id, child.Id, txtime, xid);
            }

            locklist?.ValidateLockList(parent, Perm.CREATE, child, Perm.WRITE);

            this.UpdateStat(parent, xid, txtime, ChangeKind.ChildrenAdded);
            parent.Node.AddChild(child.Node);

            parent.Node.ScheduleTriggerWatchers(ChangeKind.ChildrenAdded, path, locklist);
        }

        /// <inheritdoc />
        void IUnsafeTreeAccess.DoNodeForAddChildren(IPersistedData parent, List<IPersistedData> children)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (children == null)
            {
                throw new ArgumentNullException(nameof(children));
            }

            parent.Node.AddChildren(children);
        }

        /// <summary>
        /// Unsafe. Add ephemeral child.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="path">Path of the node to add</param>
        void IUnsafeTreeAccess.UnsafeAddEphemeralChild(IPersistedData parent, string path)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            parent.Node.ScheduleTriggerWatchers(ChangeKind.ChildrenAdded, path, null);
        }

        /// <summary>
        /// Fixes parent and child stats and node structures when a child is removed
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="node">The node.</param>
        /// <param name="txtime">The tx_time.</param>
        /// <param name="path">The path.</param>
        /// <param name="xid">The tx_id.</param>
        /// <param name="locklist">The locklist for this session.</param>
        void IUnsafeTreeAccess.UnsafeRemove(IPersistedData parent, IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            ((IUnsafeTreeAccess)this).UnsafeRemoveChild(parent, node, txtime, path, xid, locklist, false);
            ((IUnsafeTreeAccess)this).UnsafeDeleteNode(parent, node, txtime, path, xid, locklist, false);

            node.Node.ScheduleTriggerWatchers(ChangeKind.NodeDeleted, path, locklist);
            parent.Node.ScheduleTriggerWatchers(ChangeKind.ChildrenRemoved, this.GetParentPath(path), locklist);
        }

        /// <summary>
        /// Fixes parent and child stats and node structures when a child is removed
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="node">The node.</param>
        /// <param name="txtime">The tx_time.</param>
        /// <param name="path">The path.</param>
        /// <param name="xid">The tx_id.</param>
        /// <param name="locklist">The locklist for this session.</param>
        /// <param name="triggerWatcher">Whether to trigger watcher</param>
        void IUnsafeTreeAccess.UnsafeRemoveChild(IPersistedData parent, IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist, bool triggerWatcher)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (locklist == null && this.IsPathLockedDown(path))
            {
                throw new InvalidAclException(path, "lockdown");
            }

            if (ForceWB || locklist == null)
            {
                this.secondarypreprocessor.AppendRemoveChild(parent.Id, node.Id, txtime, xid);
            }

            locklist?.ValidateLockList(parent, Perm.WRITE, node, Perm.WRITE);

            this.UpdateStat(parent, xid, txtime, ChangeKind.ChildrenRemoved);

            parent.Node.RemoveChild(node.Name);

            if (triggerWatcher)
            {
                parent.Node.ScheduleTriggerWatchers(ChangeKind.ChildrenRemoved, this.GetParentPath(path), locklist);
            }
        }

        /// <summary>
        /// Fixes parent and child stats and node structures when a child is removed
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="node">The node.</param>
        /// <param name="txtime">The tx_time.</param>
        /// <param name="path">The path.</param>
        /// <param name="xid">The tx_id.</param>
        /// <param name="locklist">The locklist for this session.</param>
        /// <param name="triggerWatcher">Whether to trigger watcher</param>
        void IUnsafeTreeAccess.UnsafeDeleteNode(IPersistedData parent, IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist, bool triggerWatcher)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (locklist == null && this.IsPathLockedDown(path))
            {
                throw new InvalidAclException(path, "lockdown");
            }

            if (ForceWB || locklist == null)
            {
                this.secondarypreprocessor.AppendDelete(parent.Id, node.Id, txtime, xid);
            }

            locklist?.ValidateLockList(parent, Perm.WRITE, node, Perm.WRITE);

            this.UpdateStat(node, xid, txtime, ChangeKind.NodeDeleted);
            if (node.IsEphemeral)
            {
                this.EphemeralFactory.Delete(node);
            }
            else
            {
                this.Factory.Delete(node);
            }

            if (triggerWatcher)
            {
                node.Node.ScheduleTriggerWatchers(ChangeKind.NodeDeleted, path, locklist);
            }
        }

        /// <summary>
        /// Fixes child stats and node structures when a node is created
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="txtime">The tx_time.</param>
        /// <param name="path">The path.</param>
        /// <param name="xid">The tx_id.</param>
        /// <param name="locklist">The locklist for this session.</param>
        void IUnsafeTreeAccess.UnsafeCreate(IPersistedData node, long txtime, string path, long xid, ILockListTransaction locklist)
        {
            if (locklist == null && this.IsPathLockedDown(path))
            {
                throw new InvalidAclException(path, "lockdown");
            }

            if (ForceWB || locklist == null)
            {
                this.secondarypreprocessor.AppendCreate(node, txtime, xid);
            }

            this.UpdateStat(node, xid, txtime, ChangeKind.NodeCreated);
            this.DoNodeForCreate(node);

            node.Node.ScheduleTriggerWatchers(ChangeKind.NodeCreated, path, locklist);
        }

        /// <summary>
        /// Fixes child stats and node structures when acl's change
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="list">The list.</param>
        /// <param name="txtime">The tx_time.</param>
        /// <param name="path">The path.</param>
        /// <param name="xid">The tx_id.</param>
        /// <param name="locklist">The locklist for this session.</param>
        void IUnsafeTreeAccess.UnsafeSetAcl(IPersistedData node, IReadOnlyList<Acl> list, long txtime, string path, long xid, ILockListTransaction locklist)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (locklist == null && this.IsPathLockedDown(path))
            {
                throw new InvalidAclException(path, "lockdown");
            }

            if (ForceWB || locklist == null)
            {
                this.secondarypreprocessor.AppendSetAcl(node.Id, list, txtime, xid);
            }

            locklist?.ValidateLockList(null, Perm.NONE, node, Perm.WRITE);

            this.UpdateStat(node, xid, txtime, ChangeKind.AclChanged);
            node.Node.SetAcl(list);

            node.Node.ScheduleTriggerWatchers(ChangeKind.AclChanged, path, locklist);
        }

        /// <summary>
        /// Applies on the secondary the poison pill, if it is allowed in this configuration. Otherwise, does nothing.
        /// WARNING: THIS API IS NOT TO BE INVOKED EXCEPT FOR VALIDATING POISON PILLS!!!
        /// </summary>
        /// <param name="node">the PD this poison pill applies to</param>
        /// <param name="spec">the spec of the poison pill</param>
        /// <param name="txtime">the time for this tx</param>
        /// <param name="xid">the transdaction id</param>
        void IUnsafeTreeAccess.UnsafeSetPoisonPill(IPersistedData node, string spec, long txtime, long xid)
        {
            if (!ArePoisonPillAllowed)
            {
                return;
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            Trace.WriteLine(string.Format("Executing Poison Pill: {0} {1} {2} {3}", node.Name, spec, xid, txtime));

            // we will do things about the spec later. for now, just throw something when this is invoked.
            throw new InvalidOperationException(spec);
        }

        /// <summary>
        /// Fixes child stats and node structures when data (byte[]) change
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="data">The data.</param>
        /// <param name="txtime">The tx_time.</param>
        /// <param name="path">The path.</param>
        /// <param name="xid">The tx_id.</param>
        /// <param name="locklist">The locklist for this session.</param>
        void IUnsafeTreeAccess.UnsafeSetData(IPersistedData node, byte[] data, long txtime, string path, long xid, ILockListTransaction locklist)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (locklist == null && this.IsPathLockedDown(path))
            {
                throw new InvalidAclException(path, "lockdown");
            }

            if (ForceWB || locklist == null)
            {
                this.secondarypreprocessor.AppendSetData(node.Id, data, txtime, xid);
            }

            // It is possible for this setData to be a command for a replica.
            // if so, we will give it to the secondaryprocessor and let it decide what to do with it
            if (node.Name.Length >= 2 && node.Name[0] == '$' && node.Name[1] == '$' && this.secondarypreprocessor.ThisReplicaName != null)
            {
                this.secondarypreprocessor.TryRunCommand(node, data, txtime, xid);
            }

            locklist?.ValidateLockList(null, Perm.NONE, node, Perm.WRITE);
            int delta = -node.Stat.DataLength;
            if (data != null)
            {
                delta += data.Length;
            }

            this.UpdateStat(node, xid, txtime, ChangeKind.DataChanged, delta);
            node.Node.SetData(data);

            if (node.IsEphemeral)
            {
                this.EphemeralFactory.RecordDataDelta(delta);
            }
            else
            {
                if (locklist == null)
                {
                    this.Factory.RecordDataDelta(delta);
                }
                else
                {
                    locklist.RunOnCommit(() =>
                    {
                        this.Factory.RecordDataDelta(delta);
                    });
                }
            }

            node.Node.ScheduleTriggerWatchers(ChangeKind.DataChanged, path, locklist);
        }

        /// <summary>
        /// Unsafe. Clear tree.
        /// </summary>
        void IUnsafeTreeAccess.UnsafeClearTree()
        {
            RingMasterThreadPool.Instance.QueueUserWorkItem(ign =>
            {
                long lastXId;
                this.Root = this.Factory.LoadTree(CancellationToken.None, out lastXId);
                this.xidProvider = new UIdProvider(lastXId);
            });
        }
    }
}
