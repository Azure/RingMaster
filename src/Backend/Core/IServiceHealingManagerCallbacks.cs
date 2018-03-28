// <copyright file="IServiceHealingManagerCallbacks.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Collections.Generic;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Callback used by service healing manager
    /// </summary>
    public interface IServiceHealingManagerCallbacks
    {
        /// <summary>
        /// return the runtime members
        /// </summary>
        /// <returns>the list of members as per the runtime</returns>
        ClusterMember[] GetRuntimeMemberset();

        /// <summary>
        /// return the cluster members
        /// </summary>
        /// <returns>the list of members as agreed by the cluster members themselves (e.g. the codex)</returns>
        ClusterMember[] GetClusterMemberset();

        /// <summary>
        /// establishes a candidate for the new member set.
        /// </summary>
        /// <param name="clusterMemberset">cluste memberset</param>
        /// <param name="proposedMemberset">proposed memberset</param>
        void EnableNewRuntimeMemberset(List<ClusterMember> clusterMemberset, List<ClusterMember> proposedMemberset);
    }
}