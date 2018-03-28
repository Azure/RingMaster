// <copyright file="ServiceHealingManager.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Class ServiceHealingManager. Makes decissions about the replicaset during service healing
    /// </summary>
    public class ServiceHealingManager
    {
        private readonly IServiceHealingManagerCallbacks callbacks;
        private Timer timer = null;
        private int periodInMillis = 15000;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHealingManager"/> class.
        /// </summary>
        /// <param name="callbacks">Callback</param>
        public ServiceHealingManager(IServiceHealingManagerCallbacks callbacks)
        {
            this.callbacks = callbacks;
        }

        /// <summary>
        /// Action can be performed on a member set
        /// </summary>
        public enum MembersetAction
        {
#pragma warning disable SA1602, CS1591 // add doc later
            None = 0,
            DoableServiceHealing,
            DoableScaleDown,
            DoableScaleOut,
            UndoableChange,
#pragma warning restore
        }

        /// <summary>
        /// returns the mapping of changes (old memberId--> new RuntimeMember).
        /// </summary>
        /// <param name="clusterMembers">codex members</param>
        /// <param name="runtimeMembers">runtime members</param>
        /// <returns>the mapping</returns>
        public static Dictionary<string, ClusterMember> GetMapping(List<ClusterMember> clusterMembers, List<ClusterMember> runtimeMembers)
        {
            if (clusterMembers == null)
            {
                throw new ArgumentNullException("clusterMembers");
            }

            if (runtimeMembers == null)
            {
                throw new ArgumentNullException("runtimeMembers");
            }

            Dictionary<string, ClusterMember> mapping = new Dictionary<string, ClusterMember>();
            for (int i = 0; i < clusterMembers.Count; i++)
            {
                // select the codex member in the array
                ClusterMember codexMember = clusterMembers[i];

                // locate the runtime memberset from the list for this codex member
                ClusterMember m = runtimeMembers.Single(c => c.MemberId == codexMember.MemberId);

                if (m.Address.Equals(codexMember.Address))
                {
                    continue;
                }

                mapping.Add(m.MemberId, m);
            }

            // now we add any runtime member that is not in the original mapping (i.e. we are doing a scale out).
            foreach (ClusterMember m in runtimeMembers)
            {
                if (!mapping.ContainsKey(m.MemberId))
                {
                    mapping.Add(m.MemberId, m);
                }
            }

            return mapping;
        }

        /// <summary>
        /// Convers a list of cluster members to a string
        /// </summary>
        /// <param name="members">List of members to convert</param>
        /// <returns>string after the conversion</returns>
        public static string ToString(IEnumerable<ClusterMember> members)
        {
            StringBuilder sb = new StringBuilder();
            string sep = string.Empty;
            sb.Append("[");

            // Sort the list of members, so we all agree on a name
            List<ClusterMember> list = new List<ClusterMember>(members);
            list.Sort((a, b) => string.Compare(a.MemberId, b.MemberId));

            foreach (ClusterMember m in list)
            {
                sb.AppendFormat("{0}{1}:{2}", sep, m.MemberId, m.Address);
                sep = ",";
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Gets member set action
        /// </summary>
        /// <param name="codexMembers">List of codex members</param>
        /// <param name="runtimeMembers">List of runtime members</param>
        /// <returns>Member set action</returns>
        public static MembersetAction GetAction(List<ClusterMember> codexMembers, List<ClusterMember> runtimeMembers)
        {
            if (codexMembers == null)
            {
                throw new ArgumentNullException("codexMembers");
            }

            if (runtimeMembers == null)
            {
                throw new ArgumentNullException("runtimeMembers");
            }

            int numMembers = codexMembers.Count;

            if (numMembers != runtimeMembers.Count)
            {
                return GetActionForResize(codexMembers, runtimeMembers);
            }

            // Compare difference. if minority changed, return membersetchanged, otherwise undoable
            int maxDif = ((int)(numMembers / 2)) + 1;
            int numDif = 0;

            for (int i = 0; i < numMembers; i++)
            {
                if (codexMembers[i].MemberId != runtimeMembers[i].MemberId)
                {
                    // a member Id is different. not allowed
                    return MembersetAction.UndoableChange;
                }

                // if the address changed, take note
                if (!codexMembers[i].Address.Equals(runtimeMembers[i].Address))
                {
                    numDif++;

                    // if we have too many changes, not supported.
                    if (numDif >= maxDif)
                    {
                        return MembersetAction.UndoableChange;
                    }
                }
            }

            // no dif, nothing to do
            if (numDif == 0)
            {
                return MembersetAction.None;
            }

            // if we are here, this is a supported service healing.
            return MembersetAction.DoableServiceHealing;
        }

        /// <summary>
        /// Gets the member set action for resize
        /// </summary>
        /// <param name="codexMembers">List of codex members</param>
        /// <param name="runtimeMembers">List of runtime members</param>
        /// <returns>Action to resize</returns>
        public static MembersetAction GetActionForResize(List<ClusterMember> codexMembers, List<ClusterMember> runtimeMembers)
        {
            if (codexMembers == null)
            {
                throw new ArgumentNullException("codexMembers");
            }

            if (runtimeMembers == null)
            {
                throw new ArgumentNullException("runtimeMembers");
            }

            int numMembersOld = codexMembers.Count;
            int numMembersNew = runtimeMembers.Count;

            if (numMembersNew == numMembersOld)
            {
                throw new InvalidOperationException("GetActionForResize can only be invoked for changes in memberset size");
            }

            int newMajority = ((int)(numMembersNew / 2)) + 1;

            if (numMembersNew > numMembersOld)
            {
                if ((numMembersNew % 2) == 0)
                {
                    // no even number please
                    return MembersetAction.UndoableChange;
                }

                if (newMajority > numMembersOld)
                {
                    return MembersetAction.UndoableChange;
                }
            }
            else
            {
                // srink is not allowed
                return MembersetAction.UndoableChange;
            }

            HashSet<ClusterMember> newOnes = new HashSet<ClusterMember>(MemberComparer.Instance);

            foreach (ClusterMember m in runtimeMembers)
            {
                newOnes.Add(m);
            }

            foreach (ClusterMember m in codexMembers)
            {
                newOnes.Remove(m);
            }

            HashSet<ClusterMember> removedOnes = new HashSet<ClusterMember>(MemberComparer.Instance);
            foreach (ClusterMember m in codexMembers)
            {
                newOnes.Add(m);
            }

            foreach (ClusterMember m in runtimeMembers)
            {
                newOnes.Remove(m);
            }

            // newOnes now contains the elements added in the new set
            // removedOnes contains the elements removed from the old set
            if (removedOnes.Count > 0)
            {
                // we don't allow removal + addition at once
                return MembersetAction.UndoableChange;
            }

            // if we added more than allowed, complain
            if (newOnes.Count > newMajority)
            {
                return MembersetAction.UndoableChange;
            }

            // if we are here, this is a supported resizing.
            return MembersetAction.DoableScaleOut;
        }

        /// <summary>
        /// Stops this instance
        /// </summary>
        public void Stop()
        {
            Timer t = this.timer;
            if (t == null)
            {
                return;
            }

            t.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Starts this instance
        /// </summary>
        public void Start()
        {
            if (this.timer != null)
            {
                return;
            }

            this.timer = new Timer(this.OnTimer, null, this.periodInMillis, Timeout.Infinite);
        }

        private static List<ClusterMember> Normalize(ClusterMember[] array)
        {
            if (array == null)
            {
                return null;
            }

            List<ClusterMember> res = new List<ClusterMember>(array);
            res.Sort((m1, m2) => { return m1.MemberId.CompareTo(m2.MemberId); });
            return res;
        }

        private void OnTimer(object ign)
        {
            Timer t = this.timer;

            if (t == null)
            {
                return;
            }

            try
            {
                List<ClusterMember> runtimeMembers;
                List<ClusterMember> codexMembers;

                runtimeMembers = Normalize(this.callbacks.GetRuntimeMemberset());
                if (runtimeMembers == null)
                {
                    return;
                }

                codexMembers = Normalize(this.callbacks.GetClusterMemberset());
                if (codexMembers == null)
                {
                    return;
                }

                MembersetAction action = GetAction(codexMembers, runtimeMembers);
                bool postNewRuntime = false;
                switch (action)
                {
                    case MembersetAction.None:
                        break;
                    case MembersetAction.DoableServiceHealing:
                        postNewRuntime = true;
                        break;
                    case MembersetAction.DoableScaleOut:
                        postNewRuntime = true;
                        break;
                    case MembersetAction.DoableScaleDown:
                        Trace.TraceWarning("We should not have DoableScaleDown enabled");
                        break;
                    case MembersetAction.UndoableChange:
                        Trace.TraceWarning("Undoable Memberset change required: {0} - {1}", ToString(codexMembers), ToString(runtimeMembers));
                        break;
                    default:
                        Trace.TraceWarning("Unknown MembersetAction {0}", action);
                        break;
                }

                if (postNewRuntime)
                {
                    this.callbacks.EnableNewRuntimeMemberset(codexMembers, runtimeMembers);
                }
                else
                {
                    this.callbacks.EnableNewRuntimeMemberset(null, null);
                }
            }
            finally
            {
                t.Change(this.periodInMillis, Timeout.Infinite);
            }
        }

        private class MemberComparer : IEqualityComparer<ClusterMember>
        {
            public static MemberComparer Instance { get; } = new MemberComparer();

            public bool Equals(ClusterMember x, ClusterMember y)
            {
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return ReferenceEquals(x, y);
                }

                if (x.MemberId != y.MemberId)
                {
                    // a member Id is different. not same
                    return false;
                }

                // if the address changed, take note
                if (!x.Address.Equals(y.Address))
                {
                    return false;
                }

                return true;
            }

            public int GetHashCode(ClusterMember obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                int hash = 0;
                if (obj.MemberId != null)
                {
                    hash = obj.MemberId.GetHashCode();
                }

                if (obj.Address != null)
                {
                    hash ^= obj.Address.GetHashCode();
                }

                return hash;
            }
        }
    }
}
