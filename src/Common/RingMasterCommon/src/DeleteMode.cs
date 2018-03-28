// <copyright file="DeleteMode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;

    /// <summary>
    /// Node create options.
    /// </summary>
    [Flags]
    public enum DeleteMode : ushort
    {
        /// <summary>
        /// no flag
        /// </summary>
        None = 0,

        /// <summary>
        /// If set, this flag allows the delete to delete a node and all its children if it is not a leaf
        /// </summary>
        CascadeDelete = 1,

        /// <summary>
        /// If set, this flag allows the delete operation to succeed even if the specified path doesn't exist
        /// </summary>
        SuccessEvenIfNodeDoesntExist = 2,

        /// <summary>
        /// If set, this flag allows the cascade delete to be accelerated by not locking anything under the root,
        /// and by sending a single replicated meta-command
        /// </summary>
        FastDelete = 4,
    }
}