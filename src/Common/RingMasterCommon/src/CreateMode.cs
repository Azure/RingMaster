// <copyright file="CreateMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;

    /// <summary>
    /// Node create options.
    /// TODO: 0x00 should be reserved to None.
    /// </summary>
    [Flags]
    public enum CreateMode : ushort
    {
        /// <summary>
        /// The node will persist even if the client session that created it terminates. Do not test this flag.
        /// </summary>
        Persistent = 0x00,

        /// <summary>
        /// The node will be deleted if the client session that created it terminates.
        /// </summary>
        Ephemeral = 0x01,

        /// <summary>
        /// The node name will automatically appended with a monotically increasing number.
        /// </summary>
        Sequential = 0x02,

        /// <summary>
        /// A Persistent node whose name will automatically appended with a monotonically increasing number.
        /// </summary>
        PersistentSequential = Persistent | Sequential,

        /// <summary>
        /// An ephemeral node whose name will automatically appended with a monotonically increasing number.
        /// </summary>
        EphemeralSequential = Ephemeral | Sequential,

        /// <summary>
        /// All missing nodes in the path to the node being created will be created. The intermediate nodes will be
        /// persistent, with no data and with the same <c>Acl</c>s as the node being created.
        /// </summary>
        AllowPathCreationFlag = 0x1000,

        /// <summary>
        /// If set, this flag allows the create operation to succeed even if the specified path already exists.
        /// Note this cannot be combined with Sequential
        /// </summary>
        SuccessEvenIfNodeExistsFlag = 0x2000,

        /// <summary>
        /// A persistent node with missing nodes in the path automatically created.
        /// </summary>
        PersistentAllowPathCreation = AllowPathCreationFlag | Persistent,

        /// <summary>
        /// A persistent sequential node with missing nodes in the path automatically created.
        /// </summary>
        PersistentSequentialAllowPathCreation = AllowPathCreationFlag | Persistent | Sequential,
    }
}