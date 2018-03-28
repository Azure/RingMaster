// <copyright file="IPersistedData.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System.Collections.Generic;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using IMutableStat = Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data.IMutableStat;

    /// <summary>
    /// Interface IPersistedData.
    /// It describes a piece of 'persisted' data. i.e. a node in ringmaster.
    /// </summary>
    public interface IPersistedData
    {
        /// <summary>
        /// Gets a value indicating whether this instance is ephemeral.
        /// </summary>
        /// <value><c>true</c> if this instance is ephemeral; otherwise, <c>false</c>.</value>
        bool IsEphemeral { get; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the parent. Root's parent is "null"
        /// </summary>
        /// <value>The parent node, or null if root.</value>
        IPersistedData Parent { get; set; }

        /// <summary>
        /// Gets or sets the stat.
        /// </summary>
        /// <value>The stat.</value>
        IMutableStat Stat { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets the acl.
        /// </summary>
        /// <value>The acl.</value>
        IReadOnlyList<Acl> Acl { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; set; }

        /// <summary>
        /// Gets the saved zxid (the zxid saved for this instance).
        /// </summary>
        /// <value>The saved zxid.</value>
        long SavedZxid { get; }

        /// <summary>
        /// Gets or sets the RM node names for this instance.
        /// </summary>
        /// <value>The node.</value>
        Node Node { get; set; }

        /// <summary>
        /// Deletes the specified instance.
        /// </summary>
        void Delete();

        /// <summary>
        /// Adds a child to this instance.
        /// </summary>
        /// <param name="child">The child.</param>
        void AddChild(IPersistedData child);

        /// <summary>
        /// Removes the child from this instance's children.
        /// </summary>
        /// <param name="child">The child.</param>
        void RemoveChild(IPersistedData child);

        /// <summary>
        /// Gets the children count.
        /// </summary>
        /// <returns>the number of children</returns>
        int GetChildrenCount();

        /// <summary>
        /// Ensures the data is fresh before reading it. May block the call until it is fresh
        /// </summary>
        /// <param name="chgs">The changelist.</param>
        void AppendRead(ref IChangeList chgs);

        /// <summary>
        /// Appends a create operation to the given change (may be null, in which case a new 'change' is created).
        /// </summary>
        /// <param name="chgs">The CHGS.</param>
        void AppendCreate(ref IChangeList chgs);

        /// <summary>
        /// Appends a addchild operation to the given change (may be null, in which case a new 'change' is created).
        /// </summary>
        /// <param name="chgs">The CHGS.</param>
        /// <param name="child">The child.</param>
        void AppendAddChild(ref IChangeList chgs, IPersistedData child);

        /// <summary>
        /// Appends a removechild operation to the given change (may be null, in which case a new 'change' is created).
        /// </summary>
        /// <param name="chgs">The CHGS.</param>
        /// <param name="child">The child.</param>
        void AppendRemoveChild(ref IChangeList chgs, IPersistedData child);

        /// <summary>
        /// records the parent node
        /// </summary>
        /// <param name="parent">the parent, not null</param>
        void AppendSetParent(IPersistedData parent);

        /// <summary>
        /// Appends the remove.
        /// </summary>
        /// <param name="chgs">The CHGS.</param>
        /// <param name="parent">The parent. If this is null, it is a "just remove" call, not removing the ndoe from its parent.</param>
        /// <param name="isRecursive">if set to <c>true</c> the deletion is recursive.</param>
        void AppendRemove(ref IChangeList chgs, IPersistedData parent, bool isRecursive = false);

        /// <summary>
        /// Appends the set acl.
        /// </summary>
        /// <param name="chgs">The CHGS.</param>
        void AppendSetAcl(ref IChangeList chgs);

        /// <summary>
        /// Appends the set data.
        /// </summary>
        /// <param name="chgs">The CHGS.</param>
        void AppendSetData(ref IChangeList chgs);

        /// <summary>
        /// Appends a poison pill for this node to the changelist.
        /// </summary>
        /// <param name="spec">Poison pill specification</param>
        /// <param name="chgs">The changelist.</param>
        void AppendPoison(string spec, ref IChangeList chgs);
    }
}
