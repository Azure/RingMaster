// <copyright file="RingMasterServerInstrumentation.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class RingMasterServerInstrumentation. This class cannot be inherited.
    /// </summary>
    public sealed class RingMasterServerInstrumentation : IRingMasterServerInstrumentation
    {
        /// <summary>
        /// The storage for the instrumentation singleton
        /// </summary>
        private static IRingMasterServerInstrumentation thisInstance = new RingMasterServerInstrumentation();

        /// <summary>
        /// Prevents a default instance of the <see cref="RingMasterServerInstrumentation"/> class from being created.
        /// </summary>
        private RingMasterServerInstrumentation()
        {
        }

        /// <summary>
        /// Gets or sets the singleton instance for the instrumentation
        /// </summary>
        public static IRingMasterServerInstrumentation Instance
        {
            get { return thisInstance; }
            set { thisInstance = value; }
        }

        /// <summary>
        /// Called when [new request].
        /// </summary>
        /// <param name="sid">The sid.</param>
        /// <param name="reqType">Type of the req.</param>
        /// <param name="callid">The callid.</param>
        public void OnNewRequest(ulong sid, string reqType, ulong callid)
        {
        }

        /// <summary>
        /// Called when [bad request].
        /// </summary>
        /// <param name="sid">The sid.</param>
        public void OnBadRequest(ulong sid)
        {
        }

        /// <summary>
        /// Called when [new session].
        /// </summary>
        /// <param name="sid">The sid.</param>
        /// <param name="clientIdentity">Client Identity</param>
        /// <param name="clientAddress">IP Address of the client</param>
        public void OnNewSession(ulong sid, string clientIdentity, string clientAddress)
        {
        }

        /// <summary>
        /// Updates the ephemeral node counts.
        /// </summary>
        /// <param name="totalDataSize">Total size of the data.</param>
        /// <param name="totalCount">The total count.</param>
        public void UpdateEphemeralNodeCounts(ulong totalDataSize, ulong totalCount)
        {
        }

        /// <summary>
        /// Updates the persistent node counts.
        /// </summary>
        /// <param name="totalDataSize">Total size of the data.</param>
        /// <param name="totalCount">The total count.</param>
        public void UpdatePersistentNodeCounts(ulong totalDataSize, ulong totalCount)
        {
        }

        /// <summary>
        /// Called when [request completed].
        /// </summary>
        /// <param name="sid">The sid.</param>
        /// <param name="reqType">Type of the req.</param>
        /// <param name="callid">The callid.</param>
        /// <param name="ok">The ok.</param>
        /// <param name="responsetimeInMillis">The responsetime in millis.</param>
        public void OnRequestCompleted(ulong sid, string reqType, ulong callid, string ok, double responsetimeInMillis)
        {
        }

        /// <summary>
        /// Called when [request dequeued].
        /// </summary>
        /// <param name="sid">The sid.</param>
        /// <param name="reqType">Type of the req.</param>
        /// <param name="callid">The callid.</param>
        public void OnRequestDequeued(ulong sid, string reqType, ulong callid)
        {
        }

        /// <summary>
        /// Called when [request enqueued].
        /// </summary>
        /// <param name="sid">The sid.</param>
        /// <param name="reqType">Type of the req.</param>
        /// <param name="callid">The callid.</param>
        public void OnRequestEnqueued(ulong sid, string reqType, ulong callid)
        {
        }

        /// <summary>
        /// Called when [session closed].
        /// </summary>
        /// <param name="sid">The sid.</param>
        /// <param name="clientIdentity">Client Identity</param>
        /// <param name="clientAddress">IP Address of the client</param>
        public void OnSessionClosed(ulong sid, string clientIdentity, string clientAddress)
        {
        }

        /// <summary>
        /// A connection was refused because quota limits were exceeded.
        /// </summary>
        /// <param name="client">IP Address of the client whose connection was refused</param>
        public void OnConnectionRefused(string client)
        {
        }

        /// <summary>
        /// Called when [SSL validation].
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="ok">if set to <c>true</c> [ok].</param>
        public void OnSslValidation(string client, bool ok)
        {
        }

        /// <summary>
        /// Called when [tx committed].
        /// </summary>
        public void OnTxCommitted()
        {
        }

        /// <summary>
        /// Called when [load state].
        /// </summary>
        /// <param name="totalMilliseconds">The total milliseconds.</param>
        public void OnLoadState(long totalMilliseconds)
        {
        }

        /// <summary>
        /// Called when [save state].
        /// </summary>
        /// <param name="totalMilliseconds">The total milliseconds.</param>
        public void OnSaveState(long totalMilliseconds)
        {
        }

        /// <summary>
        /// Called when [before load state].
        /// </summary>
        public void OnBeforeLoadState()
        {
        }

        /// <summary>
        /// Called when [before save state].
        /// </summary>
        public void OnBeforeSaveState()
        {
        }

        /// <summary>
        /// Updates the bulk watcher count.
        /// </summary>
        /// <param name="globalWatchersCount">The global watchers count.</param>
        public void UpdateBulkWatcherCount(int globalWatchersCount)
        {
        }

        /// <summary>
        /// Updates the bulk watcher execution backlog.
        /// </summary>
        /// <param name="pendingCount">The pending count.</param>
        public void UpdateBulkWatcherExecutionBacklog(int pendingCount)
        {
        }

        /// <summary>
        /// called upon authentication failed
        /// </summary>
        /// <param name="exception">Not in use</param>
        public void OnAuthFailed(InvalidAclException exception)
        {
        }

        /// <summary>
        /// during load state, we found a lost parent
        /// </summary>
        /// <param name="name">name of the created node to fake that parent</param>
        public void OnLostParent(string name)
        {
        }

        /// <summary>
        /// during load state, we found a lost orphan
        /// </summary>
        /// <param name="name">name of the created orphan</param>
        public void OnOrphanFound(string name)
        {
        }

        /// <summary>
        /// during load state, we found a lost child
        /// </summary>
        /// <param name="name">name of the created node to fake that child</param>
        public void OnLostChild(string name)
        {
        }

        /// <summary>
        /// Called when a scheduled command is finished.
        /// </summary>
        /// <param name="executionSucceeded">if set to <c>true</c> the command completed successfully.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void OnScheduledCommandFinished(bool executionSucceeded, long elapsedMilliseconds)
        {
        }

        /// <summary>
        /// Called when a scheduled command queue changes.
        /// </summary>
        /// <param name="numberInQueued">number if scheduledCommands in the queue</param>
        public void OnScheduledCommandQueueChange(long numberInQueued)
        {
        }

        /// <summary>
        /// A batch of transactions sent by the transaction manager was applied.
        /// </summary>
        /// <param name="batchId">Unique id of the batch</param>
        /// <param name="maxTransactionId">The Id of the last transaction in the batch</param>
        public void OnTransactionManagerBatchApplied(ulong batchId, ulong maxTransactionId)
        {
        }

        /// <summary>
        /// Externally assigned transaction id was not in strictly increasing order.
        /// </summary>
        /// <param name="externalTransactionId">Externally assigned Transaction Id of a request</param>
        /// <param name="expectedTransactionId">Expected transaction Id</param>
        public void OnIncorrectExternalTransactionId(ulong externalTransactionId, ulong expectedTransactionId)
        {
        }

        /// <summary>
        /// Access to a lockdown path was made in RO or RW mode.
        /// </summary>
        /// <param name="nodepath">The nodepath.</param>
        /// <param name="wasRwMode">Was the access for RW mode.</param>
        public void OnLockDownAccess(string nodepath, bool wasRwMode)
        {
        }

        /// <summary>
        /// A lockdown event was encountered.
        /// </summary>
        /// <param name="nodepaths">The nodepaths locked.</param>
        /// <param name="replicationLockedDown">True if this is a replication lockdown event.</param>
        public void OnLockDownFound(IEnumerable<string> nodepaths, bool replicationLockedDown)
        {
        }

        /// <summary>
        /// Called to indicate that the server is still alive.
        /// </summary>
        /// <param name="version">Version of the binary that the server is running</param>
        /// <param name="uptime">Time span for which the server has been alive</param>
        /// <param name="isPrimary"><c>true</c> if this server is the primary for the cluster</param>
        /// <param name="activeSessions">Number of active sessions</param>
        public void OnUpdateStatus(string version, TimeSpan uptime, bool isPrimary, int activeSessions)
        {
        }

        /// <summary>
        /// Called when a measurement on a path is done
        /// </summary>
        /// <param name="measurement">The measurement itslef (the ahsh of the subtree).</param>
        /// <param name="path">The path measured.</param>
        /// <param name="xid">The transaction id that triggered this measurement.</param>
        public void OnMeasurement(long measurement, string path, long xid)
        {
        }

        /// <summary>
        /// Called when the measurement has been completed.
        /// </summary>
        /// <param name="milliseconds">The milliseconds the full measurement required.</param>
        /// <param name="txId">The tx identifier that triggered this measurement.</param>
        public void OnMeasurementCompleted(long milliseconds, long txId)
        {
        }

        /// <summary>
        /// Called when Called when an unexpected exception occurs.
        /// </summary>
        /// <param name="component">the originator component</param>
        /// <param name="ex">The exception seen</param>
        public void OnUnexpectedException(string component, Exception ex)
        {
        }

        /// <inheritdoc />
        public void ReplicaHasCheckpointCoordinationEnabled()
        {
        }

        /// <inheritdoc />
        public void ReplicaHasCheckpointLease()
        {
        }

        /// <inheritdoc />
        public void ReplicaIsTakingCheckpoint()
        {
        }

        /// <inheritdoc />
        public void OnReplicaFinishedCheckpoint(TimeSpan elapsed)
        {
        }

        /// <summary>
        /// Called when a replica is applying a transaction.
        /// </summary>
        /// <param name="txtime">The txtime of the operation.</param>
        /// <param name="xid">The xid being applied.</param>
        public void OnApply(long txtime, long xid)
        {
        }

        /// <summary>
        /// Called when a RO or RW lock is acquired
        /// </summary>
        /// <param name="readOnly">True if the lock is for readonly operation, false for read/write operation</param>
        /// <param name="succeeded">If the lock is acquired successfully</param>
        /// <param name="level">Node level</param>
        /// <param name="elapsed">Duration to acquire the lock</param>
        public void OnAcquireLock(bool readOnly, bool succeeded, int level, TimeSpan elapsed)
        {
        }

        /// <summary>
        /// Called when the replication is done for a given request
        /// </summary>
        /// <param name="elapsed">Duration from the response is ready to the change is fully replicated</param>
        public void OnResponseWaitForReplication(TimeSpan elapsed)
        {
        }
    }
}
