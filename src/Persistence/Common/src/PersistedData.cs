// <copyright file="PersistedData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Implements functionality that is common to all implementations of <see cref="IPersistedData"/>.
    /// </summary>
    public sealed class PersistedData : IPersistedData
    {
        private IMutableStat stat;
        private IPersistedData parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedData"/> class.
        /// </summary>
        /// <param name="id">Unique Id</param>
        public PersistedData(ulong id)
            : this(id, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedData"/> class.
        /// </summary>
        /// <param name="id">Unique Id</param>
        /// <param name="unused">Unused factory object, for backward compatibility</param>
        public PersistedData(ulong id, AbstractPersistedDataFactory unused)
        {
            this.Id = id;
            this.stat = new MutableStat();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is ephemeral.
        /// </summary>
        public bool IsEphemeral => false;

        /// <summary>
        /// Gets or sets the unique id of this instance.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the stat.
        /// </summary>
        public IMutableStat Stat
        {
            get
            {
                return this.stat;
            }

            set
            {
                this.stat = FirstStat.TurnIntoFirstStatIfNeeded(value);
            }
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets the ACL.
        /// </summary>
        public IReadOnlyList<Acl> Acl { get; set; }

        /// <summary>
        /// Gets or sets the parent. Not to be set directly!
        /// </summary>
        public IPersistedData Parent
        {
            get
            {
                return this.parent;
            }

            set
            {
                this.parent = value;
                if (value != null)
                {
                    this.ParentId = this.parent.Id;
                }
                else
                {
                    this.ParentId = ulong.MaxValue;
                }
            }
        }

        /// <summary>
        /// Gets the saved id.
        /// </summary>
        public long SavedZxid => this.Stat.Mzxid;

        /// <summary>
        /// Gets or sets the node.
        /// </summary>
        public Node Node { get; set; }

        /// <summary>
        /// Gets or sets the parent Id.
        /// </summary>
        public ulong ParentId { get; set; }

        /// <summary>
        /// Clones this object for persistence
        /// </summary>
        /// <returns>Cloned object</returns>
        public PersistedData Clone()
        {
            byte[] copiedData = null;
            if (this.Data != null)
            {
                copiedData = new byte[this.Data.Length];
                Array.Copy(this.Data, copiedData, this.Data.Length);
            }

            List<Acl> copiedAcl = null;
            if (this.Acl != null)
            {
                copiedAcl = this.Acl.Select(a => new Acl(a)).ToList();
            }

            return new PersistedData(this.Id)
            {
                Name = this.Name,
                Stat = this.Stat == null ? null : new MutableStat(this.Stat),
                Data = copiedData,
                Acl = copiedAcl,
                ParentId = this.ParentId,
            };
        }

        /// <summary>
        /// Cleans up reference from parent to this object.
        /// </summary>
        public void Delete()
        {
            if (this.Parent != null)
            {
                PersistenceEventSource.Log.PersistedDataDelete(this.Id, this.Parent.Id);
                this.Parent.RemoveChild(this);
            }
        }

        /// <summary>
        /// Adds a child to this node
        /// </summary>
        /// <param name="child">The child node to add</param>
        public void AddChild(IPersistedData child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (child.Parent != null)
            {
                PersistenceEventSource.Log.PersistedDataAddChild_RemovingChildFromExistingParent(child.Parent.Id, child.Parent.Name, child.Id, child.Name);
                child.Parent.RemoveChild(child);
            }

            child.Parent = this;
            PersistenceEventSource.Log.PersistedDataAddChild(this.Id, this.Name, child.Id, child.Name, this.GetChildrenCount());
        }

        /// <summary>
        /// Removes a child from this node.
        /// </summary>
        /// <param name="child">The child node to remove</param>
        public void RemoveChild(IPersistedData child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (child.Parent == null)
            {
                PersistenceEventSource.Log.PersistedDataRemoveChild_ParentIsNull(child.Id, child.Name);
                return;
            }

            if (child.Parent != this)
            {
                PersistenceEventSource.Log.PersistedDataRemoveChild_ParentIsNotThis(child.Id, child.Name, this.Id, this.Name, child.Parent.Id, child.Parent.Name);
                throw new InvalidOperationException("PersistedData.RemoveChild Failed-NotParent");
            }

            child.Parent = null;
            PersistenceEventSource.Log.PersistedDataRemoveChild(this.Id, this.Name, child.Id, child.Name, this.GetChildrenCount());
        }

        /// <summary>
        /// Returns the number of children that this node has.
        /// </summary>
        /// <returns>The number of children</returns>
        public int GetChildrenCount()
        {
            return this.stat.NumChildren - this.stat.NumEphemeralChildren;
        }

        /// <summary>
        /// Ensures the data is fresh before reading it. May block the call until it is fresh
        /// </summary>
        /// <param name="chgs">The changelist.</param>
        public void AppendRead(IChangeList chgs)
        {
            PersistenceEventSource.Log.PersistedDataAppendRead(this.Id, this.Name);
        }

        /// <summary>
        /// Associates the creation of this instance with the given <see cref="IChangeList"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> to associate with</param>
        public void AppendCreate(IChangeList changeList)
        {
            PersistenceEventSource.Log.PersistedDataAppendCreate(this.Id, this.Name);
            this.OnCreate(changeList);
        }

        /// <summary>
        /// Appends a set parent operation
        /// </summary>
        /// <param name="parent">Parent node</param>
        public void AppendSetParent(IPersistedData parent)
        {
            PersistenceEventSource.Log.PersistedDataAppendSetParent(this.Id, this.Name);
        }

        /// <summary>
        /// Associates an update (where a child was added to this instnace) with the given <see cref="IChangeList"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> to associate with</param>
        /// <param name="child">The child that was added</param>
        public void AppendAddChild(IChangeList changeList, IPersistedData child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            PersistenceEventSource.Log.PersistedDataAppendAddChild(this.Id, this.Name, child.Id, child.Name);
            this.OnUpdate(changeList);
        }

        /// <summary>
        ///  Associates an update (where a child was removed from this instance) with the given <see cref="IChangeList"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> to associate with</param>
        /// <param name="child">The child that was removed</param>
        public void AppendRemoveChild(IChangeList changeList, IPersistedData child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            PersistenceEventSource.Log.PersistedDataAppendRemoveChild(this.Id, this.Name, child.Id, child.Name);
            var childData = (PersistedData)child;
            childData.OnUpdate(changeList);
            this.OnUpdate(changeList);
        }

        /// <summary>
        ///  Associates an update (where an ACL was set on this instance) with the given <see cref="IChangeList"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> to associate with</param>
        public void AppendSetAcl(IChangeList changeList)
        {
            PersistenceEventSource.Log.PersistedDataAppendSetAcl(this.Id, this.Name);
            this.OnUpdate(changeList);
        }

        /// <summary>
        ///  Associates an update (where data was set on this instance) with the given <see cref="IChangeList"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> to associate with</param>
        public void AppendSetData(IChangeList changeList)
        {
            PersistenceEventSource.Log.PersistedDataAppendSetData(this.Id, this.Name);
            this.OnUpdate(changeList);
        }

        /// <summary>
        ///  Associates the removal of this instance with the given <see cref="IChangeList"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> to associate with</param>
        /// <param name="parent">The parent of this instance</param>
        /// <param name="isRecursive">If <c>true</c> the removal was recursive</param>
        public void AppendRemove(IChangeList changeList, IPersistedData parent, bool isRecursive = false)
        {
            PersistenceEventSource.Log.PersistedDataAppendRemove(this.Id, this.Name, isRecursive);

            // Log the change that this node, or the child, is removed.
            this.OnRemove(changeList);

            // Log the change on the parent, including Mzxid/Mtime in stat.
            ((PersistedData)parent).OnUpdate(changeList);
        }

        /// <summary>
        /// Associates a poison pill with the given <see cref="IChangeList"/>
        /// </summary>
        /// <param name="spec">The poison pill spec</param>
        /// <param name="changeList">The <see cref="IChangeList"/> to associate with</param>
        public void AppendPoison(string spec, IChangeList changeList)
        {
            PersistenceEventSource.Log.PersistedDataAppendPoison(this.Id, this.Name, spec);
        }

        /// <summary>
        /// Serializes this instance to the given <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="binaryWriter">The <see cref="BinaryWriter"/> to serialize to</param>
        public void WriteTo(BinaryWriter binaryWriter)
        {
            if (binaryWriter == null)
            {
                throw new ArgumentNullException(nameof(binaryWriter));
            }

            lock (this)
            {
                binaryWriter.Write((ulong)this.Id);
                binaryWriter.Write((string)this.Name);

                int aclCount = this.Acl == null ? -1 : this.Acl.Count;
                binaryWriter.Write((int)aclCount);

                if (aclCount > 0)
                {
                    for (int i = 0; i < aclCount; i++)
                    {
                        binaryWriter.Write((string)this.Acl[i].Id.Identifier);
                        binaryWriter.Write((string)this.Acl[i].Id.Scheme);
                        binaryWriter.Write((int)this.Acl[i].Perms);
                    }
                }

                binaryWriter.Write((long)this.Stat.Mzxid);
                binaryWriter.Write((long)this.Stat.Czxid);
                binaryWriter.Write((long)this.Stat.Pzxid);
                binaryWriter.Write((int)this.Stat.Aversion);
                binaryWriter.Write((int)this.Stat.Version);
                binaryWriter.Write((int)this.Stat.Cversion);
                binaryWriter.Write((long)this.Stat.Mtime);
                binaryWriter.Write((long)this.Stat.Ctime);

                if (this.Data == null)
                {
                    RmAssert.IsTrue(this.Stat.DataLength == 0);
                    binaryWriter.Write((int)-1);
                }
                else
                {
                    RmAssert.IsTrue(this.Stat.DataLength == this.Data.Length);
                    binaryWriter.Write((int)this.Stat.DataLength);
                    binaryWriter.Write((byte[])this.Data);
                }

                // Note this may be called in both primary (most of time) and secondaries. The number should not
                // contain ephemeral nodes. GetChildrenCount() checks the difference between total children and number
                // of ephemeral nodes (which is 0 on secondaries), and it is the correct number on both primary and
                // secondaries.
                binaryWriter.Write((int)this.GetChildrenCount());

                ////Trace.TraceInformation($"PersistedData.Write: Id={this.Id} ParentId={this.ParentId} Name={this.Name} #Children={this.GetChildrenCount()} M={this.Stat.Mzxid} NC={this.Stat.NumChildren}/{this.stat.NumEphemeralChildren}");
                binaryWriter.Write((ulong)this.ParentId);
            }
        }

        /// <summary>
        /// Deserializes this instance from the given <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">The <see cref="BinaryReader"/> to deserialize from</param>
        public void ReadFrom(BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException(nameof(binaryReader));
            }

            this.Id = binaryReader.ReadUInt64();
            this.Name = binaryReader.ReadString();

            int aclCount = binaryReader.ReadInt32();
            if (aclCount > 0)
            {
                List<Acl> list = new List<Acl>(aclCount);

                for (int i = 0; i < aclCount; i++)
                {
                    string identifier = binaryReader.ReadString();
                    string scheme = binaryReader.ReadString();
                    int perm = binaryReader.ReadInt32();
                    Acl acl = new Acl(perm, new Id(scheme, identifier));
                    list.Add(acl);
                }

                this.Acl = list.AsReadOnly();
            }
            else
            {
                this.Acl = null;
            }

            IMutableStat loadedStat = new MutableStat();
            loadedStat.Mzxid = binaryReader.ReadInt64();
            loadedStat.Czxid = binaryReader.ReadInt64();
            loadedStat.Pzxid = binaryReader.ReadInt64();
            loadedStat.Aversion = binaryReader.ReadInt32();
            loadedStat.Version = binaryReader.ReadInt32();
            loadedStat.Cversion = binaryReader.ReadInt32();
            loadedStat.Mtime = binaryReader.ReadInt64();
            loadedStat.Ctime = binaryReader.ReadInt64();

            loadedStat.DataLength = binaryReader.ReadInt32();
            if (loadedStat.DataLength == -1)
            {
                loadedStat.DataLength = 0;
                this.Data = null;
            }
            else
            {
                this.Data = binaryReader.ReadBytes(loadedStat.DataLength);
            }

            // NumEphemeralChildren is 0 after deserialization.
            loadedStat.NumChildren = binaryReader.ReadInt32();
            this.ParentId = binaryReader.ReadUInt64();

            this.Stat = loadedStat;
            ////Trace.TraceInformation($"PersistedData.Read: Id={this.Id} ParentId={this.ParentId} Name={this.Name} #Children={loadedStat.NumChildren} M={this.Stat.Mzxid}");
        }

        /// <summary>
        /// Record creation of that this instance of <see cref="IPersistedData"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> associated with the creation</param>
        private void OnCreate(IChangeList changeList)
        {
            var list = (ChangeList)changeList;
            PersistenceEventSource.Log.RecordAddition(list.Id, this.Id, this.Name, this.Stat.Czxid);
            list.RecordAdd(this);
        }

        /// <summary>
        /// Record an update to this instance of <see cref="IPersistedData"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> associated with the update</param>
        private void OnUpdate(IChangeList changeList)
        {
            var list = (ChangeList)changeList;
            PersistenceEventSource.Log.RecordUpdate(list.Id, this.Id, this.Name, this.Stat.Mzxid, this.Stat.Pzxid);
            list.RecordUpdate(this);
        }

        /// <summary>
        /// Record removal of this instance of <see cref="IPersistedData"/>.
        /// </summary>
        /// <param name="changeList">The <see cref="IChangeList"/> associated with the removal</param>
        private void OnRemove(IChangeList changeList)
        {
            var list = (ChangeList)changeList;
            PersistenceEventSource.Log.RecordRemoval(list.Id, this.Id, this.Name);
            list.RecordRemove(this);
        }
    }
}
