// <copyright file="ClusterState.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// State of the cluster
    /// </summary>
    public class ClusterState
    {
        private readonly string thisMemberId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterState"/> class.
        /// </summary>
        /// <param name="factory">Persisted data factory object</param>
        public ClusterState(IPersistedDataFactory<Node> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            this.thisMemberId = factory.Name;
            this.MemberSet = factory.GetAgreedMembers();
            this.MembersHealth = factory.GetHealth();
        }

        /// <summary>
        /// Gets the cluster members
        /// </summary>
        public ClusterMember[] MemberSet { get; private set; }

        /// <summary>
        /// Gets the cluster member health
        /// </summary>
        public IDictionary<string, HealthDefinition> MembersHealth { get; private set; }
    }
}
