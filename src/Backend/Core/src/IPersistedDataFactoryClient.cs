// <copyright file="IPersistedDataFactoryClient.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface IPersistedDataFactoryClient.
    /// Notifications to the consumer of the persistedDataFactory.
    /// </summary>
    public interface IPersistedDataFactoryClient
    {
        /// <summary>
        /// Called when this client just became primary.
        /// </summary>
        void OnBecomePrimary();

        /// <summary>
        /// Called when this client just lost primary status.
        /// </summary>
        /// <returns>async task</returns>
        Task OnPrimaryStatusLost();

        /// <summary>
        /// Indicates whether this client can become primary at this time.
        /// </summary>
        /// <returns><c>true</c> if this instance [can become primary]; otherwise, <c>false</c>.</returns>
        bool CanBecomePrimary();

        /// <summary>
        /// Called when the replication layer decides it is unsafe to allow any change on the given paths
        /// </summary>
        /// <param name="pathsToLockDown">The paths to lock down.</param>
        /// <param name="pathsFailed">The paths failed. A null here means all paths to lock down failed</param>
        /// <param name="poisonPillAvoided">if true, the poison pill was avoided</param>
        /// <param name="replicationIsInReadOnly">if true, this means the replication layer is not in ReadOnly</param>
        void OnPathsLockdownRequested(IEnumerable<string> pathsToLockDown, IEnumerable<string> pathsFailed, bool poisonPillAvoided, ref bool replicationIsInReadOnly);
    }
}
