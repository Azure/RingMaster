// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="ClusterState.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    public class ClusterState
    {
        public readonly string ThisMemberId;
        public ClusterMember[] MemberSet { get; private set; }
        public IDictionary<string, HealthDefinition> MembersHealth { get; private set; }

        public ClusterState(IPersistedDataFactory<Node> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            this.ThisMemberId = factory.Name;
            this.MemberSet = factory.GetAgreedMembers();
            this.MembersHealth = factory.GetHealth();
        }
    }
}