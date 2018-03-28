// <copyright file="RingMasterRequestType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Types of RingMaster requests.
    /// </summary>
    public enum RingMasterRequestType : ushort
    {
        /// <summary>
        /// No request.
        /// </summary>
        None = 0,

        /// <summary>
        /// Initialize a client session.
        /// </summary>
        Init,

        /// <summary>
        /// Create a node.
        /// </summary>
        Create,

        /// <summary>
        /// Delete a node.
        /// </summary>
        Delete,

        /// <summary>
        /// Synchronize with all operations on a node.
        /// </summary>
        Sync,

        /// <summary>
        /// Check if a node exists.
        /// </summary>
        Exists,

        /// <summary>
        /// Retrieve the list of <see cref="Acl"/>s associated with a node.
        /// </summary>
        GetAcl,

        /// <summary>
        /// Retrieve the data associated with a node.
        /// </summary>
        GetData,

        /// <summary>
        /// Retrieve the list of children of a node.
        /// </summary>
        GetChildren,

        /// <summary>
        /// Modify the data of a node.
        /// </summary>
        SetData,

        /// <summary>
        /// Modify the list of <see cref="Acl"/>s associated with a node.
        /// </summary>
        SetAcl,

        /// <summary>
        /// Set the credentials for a client session.
        /// </summary>
        SetAuth,

        /// <summary>
        /// Check the version of a path
        /// </summary>
        Check,

        /// <summary>
        /// Execute a number of operations atomically
        /// </summary>
        Multi,

        /// <summary>
        /// Nest one or more request with some additional metadata
        /// </summary>
        Nested,

        /// <summary>
        /// Execute a number of operations sequentially, but not atomically
        /// </summary>
        Batch,

        /// <summary>
        /// Moves a node
        /// </summary>
        Move,

        /// <summary>
        /// internal to the server only, executes an internal operation
        /// </summary>
        InternalRun,
    }
}
