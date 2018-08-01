// <copyright file="RingMasterBackendInstrumentation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Instrumentation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;

    /// <summary>
    /// An implementation of <see cref="IRingMasterServerInstrumentation"/> that emits metrics.
    /// </summary>
    public class RingMasterBackendInstrumentation : IRingMasterServerInstrumentation
    {
        /// <summary>
        /// Number of bad requests received so far.
        /// </summary>
        private readonly IMetric0D badRequests;

        /// <summary>
        /// Number of requests received so far.
        /// </summary>
        private readonly IMetric1D newRequests;

        /// <summary>
        /// Number of requests that are currently being processed.
        /// </summary>
        private readonly IMetric1D requestsExecuting;

        /// <summary>
        /// Number of failures that can be alerted for this component.
        /// </summary>
        private readonly IMetric1D alertableFailures;

        /// <summary>
        /// Number of requests that are waiting in the queue.
        /// </summary>
        private readonly IMetric1D requestsInQueue;

        /// <summary>
        /// Number of sessions that have been created during a sampling interval.
        /// </summary>
        private readonly IMetric1D sessions;

        /// <summary>
        /// Number of sessions that are currently active.
        /// </summary>
        private readonly IMetric0D activeSessions;

        /// <summary>
        /// Aggregate of the time taken to respond to requests (in milliseconds).
        /// </summary>
        private readonly IMetric1D responseTime;

        /// <summary>
        /// Number of successful SSL validations so far.
        /// </summary>
        private readonly IMetric1D sslvalidations;

        /// <summary>
        /// Number of connections refused.
        /// </summary>
        private readonly IMetric1D connectionsRefused;

        /// <summary>
        /// Number of unsuccessful SSL validations so far.
        /// </summary>
        private readonly IMetric1D sslErrors;

        /// <summary>
        /// Aggregate of the data sizes associated with the nodes.
        /// </summary>
        private readonly IMetric1D dataSize;

        /// <summary>
        /// Number of nodes.
        /// </summary>
        private readonly IMetric1D totalCount;

        /// <summary>
        /// Number of transactions that have been committed.
        /// </summary>
        private readonly IMetric0D committed;

        /// <summary>
        /// Aggregate of the time taken by the worker roles to startup.
        /// </summary>
        private readonly IMetric1D startupTime;

        /// <summary>
        /// Aggregate of the time taken by instances of this worker role
        ///  to load state from the persistent store.
        /// </summary>
        private readonly IMetric0D loadStateTimes;

        /// <summary>
        /// Aggregate of the time taken by instances of this worker role
        /// to save state to the persistent store.
        /// </summary>
        private readonly IMetric0D saveStateTimes;

        /// <summary>
        /// Number of times instances of this worker role have loaded state
        /// from the persistent store.
        /// </summary>
        private readonly IMetric0D loadStateCount;

        /// <summary>
        /// Number of times instances of this worker role have saved state to
        /// the persistent store.
        /// </summary>
        private readonly IMetric0D saveStateCount;

        /// <summary>
        /// Number of bulk watchers that are currently active.
        /// </summary>
        private readonly IMetric1D bulkWatchersCount;

        /// <summary>
        /// The bulk watcher execution backlog
        /// </summary>
        private readonly IMetric0D bulkWatcherExecutionBacklogCount;

        /// <summary>
        /// Number of authentication failures so far.
        /// </summary>
        private readonly IMetric1D authFailuresCount;

        /// <summary>
        /// Count hit of Parent nodes lost. Should always be zero
        /// </summary>
        private readonly IMetric0D lostParent;

        /// <summary>
        /// Count hit of Orphan nodes found. Should always be zero
        /// </summary>
        private readonly IMetric0D orphanFound;

        /// <summary>
        /// Count hit of children lost. Should always be zero
        /// </summary>
        private readonly IMetric0D lostChild;

        /// <summary>
        /// The Id of the last transaction manager batch that was applied.
        /// </summary>
        private readonly IMetric0D transactionManagerBatchApplied;

        /// <summary>
        /// The Id of hte last transaction manager transaction that was applied.
        /// </summary>
        private readonly IMetric0D transactionManagerTransactionApplied;

        /// <summary>
        /// Number of times externally assigned transaction Id was found to be out of sequence.
        /// </summary>
        private readonly IMetric0D incorrectExternalTransactionId;

        /// <summary>
        /// Number of accesses to lock down branches.
        /// </summary>
        private readonly IMetric2D lockDownAccess;

        /// <summary>
        /// The number of scheduled command finished, and their completion status (success/failure)
        /// </summary>
        private readonly IMetric1D scheduledCommandFinished;

        /// <summary>
        /// The times in milliseconds taken to complete scheduled commands
        /// </summary>
        private readonly IMetric0D scheduledCommandFinishedTimesMilliseconds;

        /// <summary>
        /// The number of scheduled command in the queue
        /// </summary>
        private readonly IMetric0D scheduledCommandQueued;

        /// <summary>
        /// Number of times there is a lockdown event for a branch.
        /// </summary>
        private readonly IMetric1D lockDownEvents;

        /// <summary>
        /// Number of times there is a full Lockdown.
        /// </summary>
        private readonly IMetric0D fullLockDownEvents;

        /// <summary>
        /// Number of seconds for which the service has been up.  The dimension is the
        /// current version of the role binary.
        /// </summary>
        private readonly IMetric1D uptime;

        /// <summary>
        /// Number of seconds for which this instance has been the primary.
        /// </summary>
        private readonly IMetric0D timeAsPrimary;

        /// <summary>
        /// The tx identifier of the last tx applied in this replica
        /// </summary>
        private readonly IMetric0D appliedTxId;

        /// <summary>
        /// The tx time (NOT the process time) of the last tx applied in this replica
        /// </summary>
        private readonly IMetric0D appliedTxTime;

        /// <summary>
        /// The subtree measurement (hash) for a node under the root and all its subtree
        /// </summary>
        private readonly IMetric2D subtreeMeasurement;

        /// <summary>
        /// The time in milliseconds the whole tree measurement took
        /// </summary>
        private readonly IMetric1D treeMeasurementTime;

        /// <summary>
        /// The unexpected exceptions occurred
        /// </summary>
        private readonly IMetric2D unexpectedException;

        /// <summary>
        /// If this server is the primary for the cluster this is set to 1 otherwise,
        /// this is set to 0. The dimension is the current version of the role binary.
        /// </summary>
        private readonly IMetric1D isPrimary;

        /// <summary>
        /// Pinged by <see cref="ReplicaHasCheckpointCoordinationEnabled"/>
        /// </summary>
        private readonly IMetric0D replicaHasCheckpointCoordinationEnabled;

        /// <summary>
        /// Pinged by <see cref="ReplicaHasCheckpointLease"/>.
        /// </summary>
        private readonly IMetric0D replicaHasCheckpointLease;

        /// <summary>
        /// Pinged by <see cref="ReplicaIsTakingCheckpoint"/>.
        /// </summary>
        private readonly IMetric0D replicaIsTakingCheckpoint;

        /// <summary>
        /// Used by <see cref="OnReplicaFinishedCheckpoint"/>.
        /// </summary>
        private readonly IMetric0D replicaFinishedCheckpoint;

        /// <summary>
        /// Used by <see cref="OnAcquireLock"/>
        /// </summary>
        private readonly IMetric3D acquireLock;

        /// <summary>
        /// Used by <see cref="OnResponseWaitForReplication(TimeSpan)"/>
        /// </summary>
        private readonly IMetric0D responseWaitForReplication;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterBackendInstrumentation"/> class.
        /// </summary>
        /// <param name="metricsFactory">Interface that can be used to create metrics</param>
        public RingMasterBackendInstrumentation(IMetricsFactory metricsFactory)
        {
            if (metricsFactory == null)
            {
                throw new ArgumentNullException(nameof(metricsFactory));
            }

            this.badRequests = metricsFactory.Create0D("badRequests");
            this.committed = metricsFactory.Create0D("committed");
            this.newRequests = metricsFactory.Create1D("newRequests", Dimensions.RequestType);
            this.requestsExecuting = metricsFactory.Create1D("requestsExecuting", Dimensions.RequestType);
            this.requestsInQueue = metricsFactory.Create1D("requestsInQueue", Dimensions.RequestType);
            this.responseTime = metricsFactory.Create1D("responseTime", Dimensions.RequestType);
            this.sessions = metricsFactory.Create1D("sessions", Dimensions.Client);
            this.activeSessions = metricsFactory.Create0D("activeSessions");
            this.sslvalidations = metricsFactory.Create1D("sslvalidationsPassed", Dimensions.Client);
            this.connectionsRefused = metricsFactory.Create1D("connectionsRefused", Dimensions.Client);
            this.sslErrors = metricsFactory.Create1D("sslvalidationErrors", Dimensions.Client);
            this.dataSize = metricsFactory.Create1D("dataSize", Dimensions.NodeType);
            this.totalCount = metricsFactory.Create1D("totalCount", Dimensions.NodeType);
            this.alertableFailures = metricsFactory.Create1D("alertableFailures", Dimensions.FailureType);

            this.startupTime = metricsFactory.Create1D("startupTime", Dimensions.Process);

            this.loadStateTimes = metricsFactory.Create0D("loadStateTimes");
            this.saveStateTimes = metricsFactory.Create0D("saveStateTimes");

            this.loadStateCount = metricsFactory.Create0D("loadStateCount");
            this.saveStateCount = metricsFactory.Create0D("saveStateCount");

            this.bulkWatchersCount = metricsFactory.Create1D("bulkWatchersCount", Dimensions.SessionId);

            this.bulkWatcherExecutionBacklogCount = metricsFactory.Create0D("bulkWatcherExecutionBacklogCount");
            this.authFailuresCount = metricsFactory.Create1D("authFailuresCount", Dimensions.SessionData);

            this.lostParent = metricsFactory.Create0D("lostParent");
            this.orphanFound = metricsFactory.Create0D("orphanFound");
            this.lostChild = metricsFactory.Create0D("lostChild");

            this.transactionManagerBatchApplied = metricsFactory.Create0D("transactionManagerBatchApplied");
            this.transactionManagerTransactionApplied = metricsFactory.Create0D("transactionManagerTransactionApplied");
            this.incorrectExternalTransactionId = metricsFactory.Create0D("incorrectExternalTransactionId");

            this.scheduledCommandFinished = metricsFactory.Create1D("scheduledCommandFinished", Dimensions.CompletionStatus);
            this.scheduledCommandQueued = metricsFactory.Create0D("scheduledCommandQueued");
            this.scheduledCommandFinishedTimesMilliseconds = metricsFactory.Create0D("scheduledCommandFinishedTimesMilliseconds");

            this.lockDownAccess = metricsFactory.Create2D("lockDownAccess", Dimensions.NodePath, Dimensions.AccessMode);
            this.lockDownEvents = metricsFactory.Create1D("lockDownEvents", Dimensions.NodePath);
            this.fullLockDownEvents = metricsFactory.Create0D("fullLockDownEvents");

            this.uptime = metricsFactory.Create1D("uptime", Dimensions.Version);
            this.isPrimary = metricsFactory.Create1D("isPrimary", Dimensions.Version);
            this.timeAsPrimary = metricsFactory.Create0D("timeAsPrimary");

            this.appliedTxId = metricsFactory.Create0D("appliedTxId");
            this.appliedTxTime = metricsFactory.Create0D("appliedTxTime");

            this.subtreeMeasurement = metricsFactory.Create2D("subtreeMeasurement", Dimensions.NodePath, Dimensions.TransactionId);
            this.treeMeasurementTime = metricsFactory.Create1D("treeMeasurementTime", Dimensions.TransactionId);

            this.unexpectedException = metricsFactory.Create2D("unexpectedException", Dimensions.Component, Dimensions.ExceptionType);

            this.replicaHasCheckpointCoordinationEnabled = metricsFactory.Create0D("replicaHasCheckpointCoordinationEnabled");
            this.replicaHasCheckpointLease = metricsFactory.Create0D("replicaHasCheckpointLease");
            this.replicaIsTakingCheckpoint = metricsFactory.Create0D("replicaIsTakingCheckpoint");
            this.replicaFinishedCheckpoint = metricsFactory.Create0D("replicaFinishedCheckpoint");

            this.acquireLock = metricsFactory.Create3D("acquireLock", "ReadOnly", "Succeeded", "Level");

            this.responseWaitForReplication = metricsFactory.Create0D("responseWaitForReplication");
        }

        /// <summary>
        /// Gets or sets a value indicating whether to log AcquireLock operation. This is high frequency!
        /// </summary>
        public bool LogAcquireLock { get; set; } = false;

        /// <summary>
        /// Notifies that a bad request was received from the client.
        /// </summary>
        /// <param name="sid">Id of the client session that sent the request</param>
        public virtual void OnBadRequest(ulong sid)
        {
            this.badRequests.LogValue(1);
        }

        /// <summary>
        /// Notifies that a new request has arrived.
        /// </summary>
        /// <param name="sid">Id of the client session that sent the request</param>
        /// <param name="reqType">Type of the request</param>
        /// <param name="callid">Unique Id of the request</param>
        public virtual void OnNewRequest(ulong sid, string reqType, ulong callid)
        {
            this.newRequests.LogValue(1, reqType);
        }

        /// <summary>
        /// Notifies that wire backup could not be setup.
        /// </summary>
        public virtual void OnWireBackupFailed()
        {
            this.alertableFailures.LogValue(1, KnownDimensionValues.Wirebackup);
        }

        /// <summary>
        /// Notifies that a request has been completed.
        /// </summary>
        /// <param name="sid">Id of the client session that sent the request</param>
        /// <param name="reqType">Type of the request</param>
        /// <param name="callid">Id of the request</param>
        /// <param name="ok">Result of the request</param>
        /// <param name="responsetimeInMillis">Time taken to process the request (in milliseconds)</param>
        public virtual void OnRequestCompleted(ulong sid, string reqType, ulong callid, string ok, double responsetimeInMillis)
        {
            this.responseTime.LogValue((long)responsetimeInMillis, reqType);
        }

        /// <summary>
        /// Notifies that a request has been removed from the queue in which it
        /// was waiting and is now being processed.
        /// </summary>
        /// <param name="sid">Id of the client session that sent the request</param>
        /// <param name="reqType">Type of the request</param>
        /// <param name="callid">Id of the request</param>
        public virtual void OnRequestDequeued(ulong sid, string reqType, ulong callid)
        {
            this.requestsExecuting.LogValue(1, reqType);

            // TODO: fix it later. MDM value cannot be negative
            ////this.requestsInQueue.LogValue(-1, reqType);
        }

        /// <summary>
        /// Notifies that a request has been added to the queue of
        /// waiting requests.
        /// </summary>
        /// <param name="sid">Id of the client session that sent the request</param>
        /// <param name="reqType">Type of the request</param>
        /// <param name="callid">Unique Id of the request</param>
        public virtual void OnRequestEnqueued(ulong sid, string reqType, ulong callid)
        {
            this.requestsInQueue.LogValue(1, reqType);
        }

        /// <summary>
        /// Notifies that a new session has been established with a client.
        /// </summary>
        /// <param name="sid">Id of the client session that was established</param>
        /// <param name="clientIdentity">Client Identity</param>
        /// <param name="clientAddress">IP Address of the client</param>
        public virtual void OnNewSession(ulong sid, string clientIdentity, string clientAddress)
        {
            clientAddress = clientAddress ?? "<unknown>";
            this.sessions.LogValue(1, clientAddress);
        }

        /// <summary>
        /// Notifies that the session with a client was closed.
        /// </summary>
        /// <param name="sid">Id of the session that was closed</param>
        /// <param name="clientIdentity">Client Identity</param>
        /// <param name="clientAddress">IP Address of the client</param>
        public virtual void OnSessionClosed(ulong sid, string clientIdentity, string clientAddress)
        {
            clientAddress = clientAddress ?? "<unknown>";

            // TODO: fix it later. MDM value cannot be negative
            ////this.sessions.LogValue(-1, clientAddress);
        }

        /// <summary>
        /// A connection was refused because quota limits were exceeded.
        /// </summary>
        /// <param name="client">IP Address of the client whose connection was refused</param>
        public virtual void OnConnectionRefused(string client)
        {
            client = client ?? "<unknown>";
            this.connectionsRefused.LogValue(1, client);
        }

        /// <summary>
        /// Notifies the result of SSL validation.
        /// </summary>
        /// <param name="client">Identifies the client that tried to establish the session</param>
        /// <param name="ok">Set to true if validation was successful</param>
        public virtual void OnSslValidation(string client, bool ok)
        {
            client = client ?? "<unknown>";

            if (ok)
            {
                this.sslvalidations.LogValue(1, client);
            }
            else
            {
                this.sslErrors.LogValue(1, client);
            }
        }

        /// <summary>
        /// Notifies that a transaction was committed.
        /// </summary>
        public virtual void OnTxCommitted()
        {
            this.committed.LogValue(1);
        }

        /// <summary>
        /// Notifies that this instance of the worker role is
        /// about to load state from the persistent store.
        /// </summary>
        public virtual void OnBeforeLoadState()
        {
            this.loadStateCount.LogValue(1);
        }

        /// <summary>
        /// Notifies that this instance of the worker role is about
        /// to store state to the persistent store.
        /// </summary>
        public virtual void OnBeforeSaveState()
        {
            this.saveStateCount.LogValue(1);
        }

        /// <summary>
        /// Notifies that this instance of the worker role has
        /// completed loading state from persistent store.
        /// </summary>
        /// <param name="totalMilliseconds">The number milliseconds taken to load the state</param>
        public virtual void OnLoadState(long totalMilliseconds)
        {
            // TODO: fix it later. MDM value cannot be negative
            ////this.loadStateCount.LogValue(-1);
            this.loadStateTimes.LogValue(totalMilliseconds);
        }

        /// <summary>
        /// Notifies that this instance of the worker role has completed
        /// saving state to persistent store.
        /// </summary>
        /// <param name="totalMilliseconds">The total milliseconds.</param>
        public virtual void OnSaveState(long totalMilliseconds)
        {
            // TODO: fix it later. MDM value cannot be negative
            ////this.saveStateCount.LogValue(-1);
            this.saveStateTimes.LogValue(totalMilliseconds);
        }

        /// <summary>
        /// Updates the ephemeral node counts.
        /// </summary>
        /// <param name="totalDataSize">Total size of the data.</param>
        /// <param name="totalNodes">The total number of nodes.</param>
        public virtual void UpdateEphemeralNodeCounts(ulong totalDataSize, ulong totalNodes)
        {
            this.totalCount.LogValue((long)totalNodes, "EPHEMERAL");
            this.dataSize.LogValue((long)totalDataSize, "EPHEMERAL");
        }

        /// <summary>
        /// Updates the persistent node counts.
        /// </summary>
        /// <param name="totalDataSize">Total size of the data.</param>
        /// <param name="totalNodes">The total nodes.</param>
        public virtual void UpdatePersistentNodeCounts(ulong totalDataSize, ulong totalNodes)
        {
            this.totalCount.LogValue((long)totalNodes, "PERSISTENT");
            this.dataSize.LogValue((long)totalDataSize, "PERSISTENT");
        }

        /// <summary>
        /// Reports the time taken to start this instance of the worker role.
        /// </summary>
        /// <param name="startUpTimeInFileTime">The start up time in file time.</param>
        /// <param name="entity">The entity.</param>
        public virtual void ReportStartTime(long startUpTimeInFileTime, string entity)
        {
            entity = entity ?? "<unknown>";

            this.startupTime.LogValue(startUpTimeInFileTime, entity);
        }

        /// <summary>
        /// Updates the bulk watcher count.
        /// </summary>
        /// <param name="globalWatchersCount">The global watchers count.</param>
        public virtual void UpdateBulkWatcherCount(int globalWatchersCount)
        {
            this.bulkWatchersCount.LogValue(globalWatchersCount, "global");
        }

        /// <summary>
        /// Updates the bulk watcher execution backlog.
        /// </summary>
        /// <param name="pendingCount">The pending count.</param>
        public virtual void UpdateBulkWatcherExecutionBacklog(int pendingCount)
        {
            this.bulkWatcherExecutionBacklogCount.LogValue(pendingCount);
        }

        /// <summary>
        /// Updates the counter for authentication failures.
        /// </summary>
        /// <param name="exception">Exception associated with the failure</param>
        public virtual void OnAuthFailed(InvalidAclException exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            this.authFailuresCount.LogValue(1, exception.SessionData);
        }

        /// <summary>
        /// during load state, we found a lost parent
        /// </summary>
        /// <param name="name">name of the created node to fake that parent</param>
        public virtual void OnLostParent(string name)
        {
            this.lostParent.LogValue(1);
        }

        /// <summary>
        /// during load state, we found a lost orphan
        /// </summary>
        /// <param name="name">name of the created orphan</param>
        public virtual void OnOrphanFound(string name)
        {
            this.orphanFound.LogValue(1);
        }

        /// <summary>
        /// during load state, we found a lost child
        /// </summary>
        /// <param name="name">name of the created node to fake that child</param>
        public virtual void OnLostChild(string name)
        {
            this.lostChild.LogValue(1);
        }

        /// <summary>
        /// A batch of transactions sent by the transaction manager was applied.
        /// </summary>
        /// <param name="batchId">Unique id of the batch</param>
        /// <param name="maxTransactionId">The Id of the last transaction in the batch</param>
        public virtual void OnTransactionManagerBatchApplied(ulong batchId, ulong maxTransactionId)
        {
            this.transactionManagerBatchApplied.LogValue((long)batchId);
            this.transactionManagerTransactionApplied.LogValue((long)maxTransactionId);
        }

        /// <summary>
        /// Externally assigned transaction id was not in strictly increasing order.
        /// </summary>
        /// <param name="externalTransactionId">Externally assigned Transaction Id of a request</param>
        /// <param name="expectedTransactionId">Expected transaction Id</param>
        public virtual void OnIncorrectExternalTransactionId(ulong externalTransactionId, ulong expectedTransactionId)
        {
            this.incorrectExternalTransactionId.LogValue(1);
        }

        /// <summary>
        /// Access to a lockdown path was made in RO or RW mode.
        /// </summary>
        /// <param name="nodepath">The nodepath.</param>
        /// <param name="wasRwMode">Was the access for RW mode.</param>
        public virtual void OnLockDownAccess(string nodepath, bool wasRwMode)
        {
            this.lockDownAccess.LogValue(1, nodepath, wasRwMode ? KnownDimensionValues.ReadWriteMode : KnownDimensionValues.ReadOnlyMode);
        }

        /// <summary>
        /// Called when a scheduled command is finished.
        /// </summary>
        /// <param name="executionSucceeded">if set to <c>true</c> the command completed successfully.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void OnScheduledCommandFinished(bool executionSucceeded, long elapsedMilliseconds)
        {
            this.scheduledCommandFinished.LogValue(1, executionSucceeded ? KnownDimensionValues.ScheduledCommandFinishedSuccess : KnownDimensionValues.ScheduledCommandFinishedFailure);
            this.scheduledCommandFinishedTimesMilliseconds.LogValue(elapsedMilliseconds);
        }

        /// <summary>
        /// Called when a scheduled command queue changes.
        /// </summary>
        /// <param name="numberInQueued">number if scheduledCommands in the queue</param>
        public void OnScheduledCommandQueueChange(long numberInQueued)
        {
            this.scheduledCommandQueued.LogValue(numberInQueued);
        }

        /// <summary>
        /// A lockdown event was encountered.
        /// </summary>
        /// <param name="nodepaths">The nodepaths locked.</param>
        /// <param name="replicationLockedDown">True if this is a replication lockdown event.</param>
        public virtual void OnLockDownFound(IEnumerable<string> nodepaths, bool replicationLockedDown)
        {
            if (nodepaths != null)
            {
                foreach (string p in nodepaths)
                {
                    this.lockDownEvents.LogValue(1, p);
                }
            }

            if (replicationLockedDown)
            {
                this.fullLockDownEvents.LogValue(1);
            }
        }

        /// <summary>
        /// Called to indicate that the server is still alive.
        /// </summary>
        /// <param name="version">Version of the binary that the server is running</param>
        /// <param name="uptime">Time span for which the server has been alive</param>
        /// <param name="isPrimary"><c>true</c> if this server is the primary for the cluster</param>
        /// <param name="activeSessions">Number of active sessions</param>
        public virtual void OnUpdateStatus(string version, TimeSpan uptime, bool isPrimary, int activeSessions)
        {
            this.uptime.LogValue((int)uptime.TotalSeconds, version);
            this.isPrimary.LogValue(isPrimary ? 1 : 0, version);
            this.activeSessions.LogValue(activeSessions);
        }

        /// <summary>
        /// Called to update status if this instance is a primary.
        /// </summary>
        /// <param name="timeAsPrimary">Timespan for which this instance has been primary</param>
        /// <param name="maxTimeAsPrimary">Max time span for which this instance is allowed to be primary</param>
        public virtual void OnUpdatePrimaryStatus(TimeSpan timeAsPrimary, TimeSpan maxTimeAsPrimary)
        {
            this.timeAsPrimary.LogValue((long)timeAsPrimary.TotalSeconds);
        }

        /// <summary>
        /// Called when a replica is applying a transaction.
        /// </summary>
        /// <param name="txtime">The txtime of the operation.</param>
        /// <param name="xid">The xid being applied.</param>
        public virtual void OnApply(long txtime, long xid)
        {
            this.appliedTxId.LogValue(xid);
            this.appliedTxTime.LogValue(txtime);
        }

        /// <summary>
        /// Called when a measurement on a path is done
        /// </summary>
        /// <param name="measurement">The measurement itslef (the ahsh of the subtree).</param>
        /// <param name="path">The path measured.</param>
        /// <param name="xid">The transaction id that triggered this measurement.</param>
        public virtual void OnMeasurement(long measurement, string path, long xid)
        {
            this.subtreeMeasurement.LogValue(measurement, path, xid.ToString());
        }

        /// <summary>
        /// Called when the measurement has been completed.
        /// </summary>
        /// <param name="milliseconds">The milliseconds the full measurement required.</param>
        /// <param name="txId">The tx identifier that triggered this measurement.</param>
        public virtual void OnMeasurementCompleted(long milliseconds, long txId)
        {
            this.treeMeasurementTime.LogValue(milliseconds, txId.ToString());
        }

        /// <summary>
        /// Called when an unexpected exception occurs.
        /// </summary>
        /// <param name="component">the originator component</param>
        /// <param name="ex">The exception seen</param>
        public virtual void OnUnexpectedException(string component, Exception ex)
        {
            this.unexpectedException.LogValue(1, component, ex?.GetType().Name ?? "null");
        }

        /// <inheritdoc />
        public void ReplicaHasCheckpointCoordinationEnabled()
        {
            this.replicaHasCheckpointCoordinationEnabled.LogValue(1);
        }

        /// <inheritdoc />
        public void ReplicaHasCheckpointLease()
        {
            this.replicaHasCheckpointLease.LogValue(1);
        }

        /// <inheritdoc />
        public void ReplicaIsTakingCheckpoint()
        {
            this.replicaIsTakingCheckpoint.LogValue(1);
        }

        /// <inheritdoc />
        public void OnReplicaFinishedCheckpoint(TimeSpan elapsed)
        {
            this.replicaFinishedCheckpoint.LogValue(elapsed.Ticks);
        }

        /// <inheritdoc />
        public void OnAcquireLock(bool readOnly, bool succeeded, int level, TimeSpan elapsed)
        {
            if (this.LogAcquireLock)
            {
                this.acquireLock.LogValue(
                    elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000),
                    readOnly ? "ReadOnly" : "ReadWrite",
                    succeeded ? "Succeeded" : "Failed",
                    level.ToString());
            }
        }

        /// <inheritdoc />
        public void OnResponseWaitForReplication(TimeSpan elapsed)
        {
            this.responseWaitForReplication.LogValue(elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000));
        }

        /// <summary>
        /// Known values for some dimensions.
        /// </summary>
        public struct KnownDimensionValues
        {
            /// <summary>
            /// Read only mode
            /// </summary>
            public const string ReadOnlyMode = "ReadOnlyMode";

            /// <summary>
            /// Read write mode
            /// </summary>
            public const string ReadWriteMode = "ReadWriteMode";

            /// <summary>
            /// The scheduled command finished as success
            /// </summary>
            public const string ScheduledCommandFinishedSuccess = "Success";

            /// <summary>
            /// The scheduled command finished as failure
            /// </summary>
            public const string ScheduledCommandFinishedFailure = "Failure";

            /// <summary>
            /// The wire backup component
            /// </summary>
            public const string Wirebackup = "wirebackup";

            /// <summary>
            /// Marshaling dimension value.
            /// </summary>
            public const string Marshaling = "marshaling";

            /// <summary>
            /// Un-Marshaling dimension value.
            /// </summary>
            public const string UnMarshaling = "unmarshaling";

            /// <summary>
            /// Wait Marshaling dimension value.
            /// </summary>
            public const string WaitMarshaling = "waitmarshaling";

            /// <summary>
            /// Wait Un-Marshaling dimension value.
            /// </summary>
            public const string WaitUnMarshaling = "waitunmarshaling";
        }

        /// <summary>
        /// Dimensions that can be used to categorize the measurements
        /// recorded by this class.
        /// </summary>
        private struct Dimensions
        {
            /// <summary>
            /// The client associated with the request.
            /// </summary>
            public const string Client = "client";

            /// <summary>
            /// The type of node.
            /// </summary>
            public const string NodeType = "nodetype";

            /// <summary>
            /// the type of failure
            /// </summary>
            public const string FailureType = "failuretype";

            /// <summary>
            /// The process.
            /// </summary>
            public const string Process = "process";

            /// <summary>
            /// The type of a request.
            /// </summary>
            public const string RequestType = "requestType";

            /// <summary>
            /// Data associated with a session.
            /// </summary>
            public const string SessionData = "sessionData";

            /// <summary>
            /// Unique Id of a session.
            /// </summary>
            public const string SessionId = "sessionId";

            /// <summary>
            /// Version of the executing binary.
            /// </summary>
            public const string Version = "version";

            /// <summary>
            /// The path of the node
            /// </summary>
            public const string NodePath = "nodepath";

            /// <summary>
            /// The mode the path was accessed
            /// </summary>
            public const string AccessMode = "accessmode";

            /// <summary>
            /// The completion status
            /// </summary>
            public const string CompletionStatus = "completionstatus";

            /// <summary>
            /// The transaction id this measurement was upon
            /// </summary>
            public const string TransactionId = "transactionid";

            /// <summary>
            /// The component that saw the exception
            /// </summary>
            public const string Component = "Component";

            /// <summary>
            /// The type of the exception seen
            /// </summary>
            public const string ExceptionType = "ExceptionType";
        }
    }
}
