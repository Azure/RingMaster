// <copyright file="IChangeList.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface of Change List
    /// </summary>
    public interface IChangeList
    {
        /// <summary>
        /// Commits this chagelist.
        /// </summary>
        /// <param name="xid">the transaction id for this command</param>
        /// <param name="task">async task to indicate the completion of change replication</param>
        void Commit(long xid, out Task task);

        /// <summary>
        /// Commits this chagelist synchronously.
        /// </summary>
        /// <param name="xid">the transaction id for this command</param>
        /// <param name="ev">the event to signal on completion</param>
        /// <param name="task">async task to indicate the completion of change replication</param>
        void CommitSync(long xid, ManualResetEvent ev, out Task task);

        /// <summary>
        /// Aborts this change list.
        /// </summary>
        void Abort();

        /// <summary>
        /// Sets the time (int rm time) when this changelist was initiated
        /// </summary>
        /// <param name="txTime">Transaction time</param>
        void SetTime(long txTime);
    }
}
