// <copyright file="IReplication.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Interface to an object that is used to track a pending replication.
    /// </summary>
    public interface IReplication : IDisposable
    {
        /// <summary>
        /// Gets the unique Id of the replication.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// Replicate the addition of a new <see cref="IPersistedData"/>.
        /// </summary>
        /// <param name="data">The data to be added</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        Task Add(PersistedData data);

        /// <summary>
        /// Replicate the update of an existing <see cref="IPersistedData"/>.
        /// </summary>
        /// <param name="data">The data to be updated</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        Task Update(PersistedData data);

        /// <summary>
        /// Replicate the removal of an existing <see cref="IPersistedData"/>.
        /// </summary>
        /// <param name="data">The data to be removed</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        Task Remove(PersistedData data);

        /// <summary>
        /// Commit all the accumulated changes.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        Task Commit();
    }
}
