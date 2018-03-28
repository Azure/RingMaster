// <copyright file="RepositoryOperation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    /// <summary>
    /// Origin operations.
    /// </summary>
    public enum RepositoryOperation
    {
        /// <summary>
        /// Query name operation.
        /// </summary>
        QueryName,

        /// <summary>
        /// Delegation lookup operation.
        /// </summary>
        Delegation,

        /// <summary>
        /// RingMaster get call.
        /// </summary>
        RingMasterGet,

        /// <summary>
        /// RingMaster put call.
        /// </summary>
        RingMasterWrite,

        /// <summary>
        /// RingMaster delete call.
        /// </summary>
        RingMasterDelete,

        /// <summary>
        /// RingMaster sync call.
        /// </summary>
        RingMasterSync,

        /// <summary>
        /// RingMaster multi call.
        /// </summary>
        RingMasterMulti,

        /// <summary>
        /// Resource provider read operation.
        /// </summary>
        ProviderReadOperation,

        /// <summary>
        /// Resource provider update or insert operation.
        /// </summary>
        ProviderUpsertOperation,

        /// <summary>
        /// Resource provider delete operation.
        /// </summary>
        ProviderDeleteOperation,

        /// <summary>
        /// Resource provider list operation.
        /// </summary>
        ProviderListOperation,

        /// <summary>
        /// Resource provider move operation.
        /// </summary>
        ProviderMoveOperation,

        /// <summary>
        /// ResourceProviderStatus operation.
        /// </summary>
        ProviderStatusOperation,
    }
}
