// <copyright file="RingMasterEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// RingMaster events
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Backend-RingMasterEvents")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class RingMasterEventSource : EventSource
    {
        private static readonly RingMasterEventSource LogInstance = new RingMasterEventSource();

        public RingMasterEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static RingMasterEventSource Log
        {
            get { return LogInstance; }
        }

        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.Informational, Version = 2)]
        public void ApplyBatchSucceeded(ulong timeStreamId, ulong batchId, ulong minTransactionId, ulong maxTransactionId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMaster.ApplyBatch-Succeeded timeStreamId={timeStreamId}, batchId={batchId}, minTransactionId={minTransactionId}, maxTransactionId={maxTransactionId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(2, timeStreamId, batchId, minTransactionId, maxTransactionId, elapsedMilliseconds);
            }
        }

        [Event(3, Level = EventLevel.Error, Version = 2)]
        public void ApplyBatch_ExternalTransactionIdNotInSequence(ulong timeStreamId, ulong batchId, ulong transactionId, ulong lastExternallyAssignedTransactionId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMaster.ApplyBatch-ExternalTransactionIdNotInSequence timeStreamId={timeStreamId}, batchId={batchId}, transactionId={transactionId}, lastExternallyAssignedTransactionId={lastExternallyAssignedTransactionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(3, timeStreamId, batchId, transactionId, lastExternallyAssignedTransactionId);
            }
        }

        [Event(4, Level = EventLevel.Error, Version = 2)]
        public void ApplyBatch_BatchIdNotInSequence(ulong timeStreamId, ulong batchId, ulong lastAppliedBatchId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMaster.ApplyBatch-BatchIdNotInSequence timeStreamId={timeStreamId}, batchId={batchId}, lastAppliedBatchId={lastAppliedBatchId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(4, timeStreamId, batchId, lastAppliedBatchId);
            }
        }

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested(bool poisonPillPrevented, bool replicationIsInReadOnly)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMaster.OnPathsLockdownRequested poisonPillPrevented={0}, replicationIsInReadOnly={1}", poisonPillPrevented, replicationIsInReadOnly);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(5, poisonPillPrevented, replicationIsInReadOnly);
            }
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested_FinalSet(string path)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMaster.OnPathsLockdownRequested FinalSet path={0}", path);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(6, path);
            }
        }

        [Event(7, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested_FullLockdown()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMaster.OnPathsLockdownRequested FullLockdown");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(7);
            }
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested_PartialLockdown(string path)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMaster.OnPathsLockdownRequested PartialLockdown path={0}", path);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(8, path);
            }
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void PathIsLockedDown(string path)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMaster.PathIsLockedDown path={0}", path);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(9, path);
            }
        }

        [Event(10, Level = EventLevel.Warning, Version = 2)]
        public void ApplyBatchAborted(ulong timeStreamId, ulong batchId, ulong minTransactionId, ulong maxTransactionId, ulong abortedAtTransactionId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceWarning($"RingMaster.ApplyBatch-Aborted timeStreamId={timeStreamId}, batchId={batchId}, minTransactionId={minTransactionId}, maxTransactionId={maxTransactionId}, abortedAtTransactionId={abortedAtTransactionId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(10, timeStreamId, batchId, minTransactionId, maxTransactionId, abortedAtTransactionId, elapsedMilliseconds);
            }
        }

        [Event(11, Level = EventLevel.LogAlways, Version = 1)]
        public void InitializeSession(ulong sessionId, string clientIP, string clientIdentity)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMasterBackendCore.InitializeSession sessionId={0}, clientIP={1}, clientIdentity={2}", sessionId, clientIP, clientIdentity);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11, sessionId, clientIP ?? "<null>", clientIdentity ?? "<null>");
            }
        }

        [Event(12, Level = EventLevel.LogAlways, Version = 1)]
        public void SetAuth(ulong sessionId, string clientIP, string clientIdentity, string clientDigest)
        {
            if (this.TraceLevel >= TraceLevel.Warning)
            {
                Trace.TraceInformation("RingMasterBackendCore.SetAuth sessionId={0}, clientIP={1}, clientIdentity={2}, clientDigest={3}", sessionId, clientIP, clientIdentity, clientDigest);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(12, sessionId, clientIP, clientIdentity, clientDigest);
            }
        }

        [Event(13, Level = EventLevel.LogAlways, Version = 1)]
        public void RequestCommand(ulong sessionId, string clientIP, string clientIdentity, string path)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMasterBackendCore.RequestCommand sessionId={0}, clientIP={1}, clientIdentity={2}, path={3}", sessionId, clientIP, clientIdentity, path);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(13, sessionId, clientIP, clientIdentity, path);
            }
        }

        [Event(14, Level = EventLevel.LogAlways, Version = 1)]
        public void RunCommand(string command)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("RingMasterBackendCore.RunCommand command={0}", command);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(14, command);
            }
        }

        [Event(15, Level = EventLevel.Error, Version = 2)]
        public void ProcessMessageFailed(ulong sessionId, ulong requestId, long zxid, int retryCount, long elapsedMilliseconds, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.ProcessMessage-Failed sessionId={sessionId}, requestId={requestId}, retryCount={retryCount}, elapsedMilliseconds={elapsedMilliseconds}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(15, sessionId, requestId, zxid, retryCount, elapsedMilliseconds, exception);
            }
        }

        [Event(16, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_PersistedDataFactoryNotActive(ulong sessionId, ulong requestId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.ProcessMessage-PersistedDataFactoryNotActive sessionId={sessionId}, requestId={requestId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(16, sessionId, requestId);
            }
        }

        [Event(17, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_RequestValidationFailed(ulong sessionId, ulong requestId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.ProcessMessage-RequestValidationFailed sessionId={sessionId}, requestId={requestId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(17, sessionId, requestId);
            }
        }

        [Event(18, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_FailedToAcquireExecutionQueue(ulong sessionId, ulong requestId, string executionQueueId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.ProcessMessage-FailedToAcquireExecutionQueue sessionId={sessionId}, requestId={requestId}, executionQueueId={executionQueueId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(18, sessionId, requestId, executionQueueId);
            }
        }

        [Event(19, Level = EventLevel.Informational, Version = 2)]
        public void ProcessMessageSucceeded(ulong sessionId, ulong requestId, long zxid, int requestType, string path, long elapsedMilliseconds)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(19, sessionId, requestId, zxid, requestType, path, elapsedMilliseconds);
            }
        }

        [Event(20, Level = EventLevel.Verbose, Version = 1)]
        public void RequestCreateSucceeded(ulong sessionId, ulong requestId, string path, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.RequestCreate-Succeeded sessionId={sessionId}, requestId={requestId}, path={path}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(20, sessionId, requestId, path, elapsedMilliseconds);
            }
        }

        [Event(21, Level = EventLevel.Verbose, Version = 1)]
        public void RequestGetChildrenSucceeded(ulong sessionId, ulong requestId, string path, string retrievalCondition, int childrenCount, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.RequestGetChildren-Succeeded sessionId={sessionId}, requestId={requestId}, path={path}, retrievalCondition={retrievalCondition}, childrenCount={childrenCount}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(21, sessionId, requestId, path, retrievalCondition, childrenCount, elapsedMilliseconds);
            }
        }

        [Event(22, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenEnumerationLimitExceeded(ulong sessionId, ulong requestId, string path, int childrenCount, int enumerationLimit)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestGetChildren-EnumerationLimitExceeded sessionId={sessionId}, requestId={requestId}, path={path}, childrenCount={childrenCount}, enumerationLimit={enumerationLimit}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(22, sessionId, requestId, path, childrenCount, enumerationLimit);
            }
        }

        [Event(23, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenRetrieveChildrenFailed(ulong sessionId, ulong requestId, string path, int childrenCount, string retrievalCondition, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestGetChildren-RetrieveChildrenFailed sessionId={sessionId}, requestId={requestId}, path={path}, childrenCount={childrenCount}, retrievalCondition={retrievalCondition}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(23, sessionId, requestId, path, childrenCount, retrievalCondition, exception);
            }
        }

        [Event(24, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenInvalidRetrievalCondition(string retrievalCondition)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestGetChildren-InvalidRetrievalCondition retrievalCondition={retrievalCondition}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(24, retrievalCondition);
            }
        }

        [Event(25, Level = EventLevel.Informational, Version = 1)]
        public void RingMasterNodeChildrenLimits(int enumerationLimit, int minSortedDictionaryThreshold, int maxSortedDictionaryThreshold)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"Node.ChildrenLimits enumerationLimit={enumerationLimit}, minSortedDictionaryThreshold={minSortedDictionaryThreshold}, maxSortedDictionaryThreshold={maxSortedDictionaryThreshold}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(25, enumerationLimit, minSortedDictionaryThreshold, maxSortedDictionaryThreshold);
            }
        }

        [Event(26, Level = EventLevel.Error, Version = 1)]
        public void RequestGetDataFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestGetData-Failed sessionId={sessionId}, requestId={requestId}, path={path}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(26, sessionId, requestId, path, exception);
            }
        }

        [Event(27, Level = EventLevel.Error, Version = 1)]
        public void RequestCreateFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestCreate-Failed sessionId={sessionId}, requestId={requestId}, path={path}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(27, sessionId, requestId, path, exception);
            }
        }

        [Event(28, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestGetChildren-Failed sessionId={sessionId}, requestId={requestId}, path={path}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(28, sessionId, requestId, path, exception);
            }
        }

        [Event(29, Level = EventLevel.Error, Version = 1)]
        public void RequestExistsFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestExists-Failed sessionId={sessionId}, requestId={requestId}, path={path}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(29, sessionId, requestId, path, exception);
            }
        }

        [Event(30, Level = EventLevel.Error, Version = 1)]
        public void RequestDeleteFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestDelete-Failed sessionId={sessionId}, requestId={requestId}, path={path}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(30, sessionId, requestId, path, exception);
            }
        }

        [Event(31, Level = EventLevel.Error, Version = 1)]
        public void RequestSetDataFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestSetData-Failed sessionId={sessionId}, requestId={requestId}, path={path}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(31, sessionId, requestId, path, exception);
            }
        }

        [Event(32, Level = EventLevel.Error, Version = 1)]
        public void RequestSetAclFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.RequestSetAcl-Failed sessionId={sessionId}, requestId={requestId}, path={path}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(32, sessionId, requestId, path, exception);
            }
        }

        [Event(33, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherSet(ulong sessionId, string childId, string watcherContext)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceError($"RingMasterBackendCore.WatcherSet sessionId={sessionId}, childId={childId}, watcherContext={watcherContext}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(33, sessionId, childId, watcherContext);
            }
        }

        [Event(34, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherRemoved(ulong sessionId, string childId, string watcherContext)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceError($"RingMasterBackendCore.WatcherRemoved sessionId={sessionId}, childId={childId}, watcherContext={watcherContext}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(34, sessionId, childId, watcherContext);
            }
        }

        [Event(35, Level = EventLevel.Verbose, Version = 1)]
        public void TryRemoveOnTerminateAction(ulong sessionId, string actionName, bool wasRemoved)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ClientSession.TryRemoveOnTerminateAction sessionId={sessionId}, actionName={actionName}, wasRemoved={wasRemoved}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(35, sessionId, actionName, wasRemoved);
            }
        }

        [Event(36, Level = EventLevel.Verbose, Version = 1)]
        public void RemoveOnTerminateAction(ulong sessionId, string actionName)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ClientSession.RemoveOnTerminateAction sessionId={sessionId}, actionName={actionName}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(36, sessionId, actionName);
            }
        }

        [Event(37, Level = EventLevel.Verbose, Version = 1)]
        public void AddOnTerminateAction(ulong sessionId, string actionName)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ClientSession.AddOnTerminateAction sessionId={sessionId}, actionName={actionName}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(37, sessionId, actionName);
            }
        }

        [Event(38, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_Timeout(ulong sessionId, ulong requestId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterBackendCore.ProcessMessage-Timeout sessionId={sessionId}, requestId={requestId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(38, sessionId, requestId, elapsedMilliseconds);
            }
        }

        [Event(39, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_Start(int threadCount)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterRequestExecutor.Start threadCount={threadCount}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(39, threadCount);
            }
        }

        [Event(40, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_ProcessPendingRequestsThreadStarted(int threadId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterRequestExecutor.ProcessPendingRequests ThreadStarted threadId={threadId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(40, threadId);
            }
        }

        [Event(41, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_ProcessPendingRequestsThreadStopped(int threadId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterRequestExecutor.ProcessPendingRequests ThreadStopped threadId={threadId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(41, threadId);
            }
        }

        [Event(42, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_Stopping(int threadCount)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterRequestExecutor.Stop Stopping threadCount={threadCount}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(42, threadCount);
            }
        }

        [Event(43, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_Stopped()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterRequestExecutor.Stop Stopped");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(43);
            }
        }

        [Event(44, Level = EventLevel.Error, Version = 1)]
        public void Executor_RequestQueueOverflow(long sequenceNumber, ulong sessionId, ulong requestId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestExecutor.RequestQueueOverflow sequenceNumber={sequenceNumber}, sessionId={sessionId}, requestId={requestId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(44, sequenceNumber, sessionId, requestId);
            }
        }

        [Event(45, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessRequestFailed(long sequenceNumber, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestExecutor.ProcessRequestFailed sequenceNumber={sequenceNumber}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(45, sequenceNumber, exception);
            }
        }

        [Event(46, Level = EventLevel.Informational, Version = 1)]
        public void Executor_ProcessRequestCompleted(long sequenceNumber, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestExecutor.ProcessRequestCompleted sequenceNumber={sequenceNumber}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(46, sequenceNumber, elapsedMilliseconds);
            }
        }

        [Event(47, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessRequestCancelled(long sequenceNumber)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestExecutor.ProcessRequestCancelled sequenceNumber={sequenceNumber}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(47, sequenceNumber);
            }
        }

        [Event(48, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessRequestTimedout(long sequenceNumber)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestExecutor.ProcessRequestTimedout sequenceNumber={sequenceNumber}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(48, sequenceNumber);
            }
        }

        [Event(49, Level = EventLevel.Informational, Version = 1)]
        public void Executor_RequestQueued(long sequenceNumber, ulong sessionId, ulong requestId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestExecutor.RequestQueued sequenceNumber={sequenceNumber}, sessionId={sessionId}, requestId={requestId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(49, sequenceNumber, sessionId, requestId);
            }
        }

        [Event(50, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessPendingRequestsThreadFailed(int threadId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestExecutor.ProcessPendingRequests ThreadFailed threadId={threadId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(50, threadId, exception);
            }
        }

        [Event(51, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemoveExistingWatcher(ulong sessionId, string watchedPath)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection RemoveExistingWatcher sessionId={sessionId}, watchedPath={watchedPath}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(51, sessionId, watchedPath);
            }
        }

        [Event(52, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_AddWatcher(ulong sessionId, string watchedPath, int watcherCount, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection AddWatcher sessionId={sessionId}, watchedPath={watchedPath}, watcherCount={watcherCount}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(52, sessionId, watchedPath, watcherCount, elapsedMilliseconds);
            }
        }

        [Event(53, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemoveWatcher(ulong sessionId, string watchedPath, int watcherCount, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection RemoveWatcher sessionId={sessionId}, watchedPath={watchedPath}, watcherCount={watcherCount}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(53, sessionId, watchedPath, watcherCount, elapsedMilliseconds);
            }
        }

        [Event(54, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherCollection_WatcherApplies(string path, ulong sessionId, string watchedPath)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection WatcherApplies path={path}, sessionId={sessionId}, watchedPath={watchedPath}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(54, path, sessionId, watchedPath);
            }
        }

        [Event(55, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_EnumeratedApplicableWatchers(string path, int applicableWatchersCount, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection EnumeratedApplicableWatchers path={path}, applicableWatchersCount={applicableWatchersCount}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(55, path, applicableWatchersCount, elapsedMilliseconds);
            }
        }

        [Event(56, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemoveWatcherOnSessionTermination(ulong sessionId, string watchedPath, int watcherCount)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection RemoveWatcherOnSessionTermination sessionId={sessionId}, watchedPath={watchedPath}, watcherCount={watcherCount}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(56, sessionId, watchedPath, watcherCount);
            }
        }

        [Event(57, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_CleanupOnSessionTermination(ulong sessionId, string watchedPath)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection CleanupOnSessionTermination sessionId={sessionId}, watchedPath={watchedPath}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(57, sessionId, watchedPath);
            }
        }

        [Event(58, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemovedAllWatchersForSession(ulong sessionId, int removedCount, int watcherCount, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterBackendCore.WatcherCollection RemovedAllWatchersForSession sessionId={sessionId}, removedCount={removedCount}, watcherCount={watcherCount}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(58, sessionId, removedCount, watcherCount, elapsedMilliseconds);
            }
        }

        [Event(59, Level = EventLevel.Informational, Version = 1)]
        public void ScheduleMultiCompleted(ulong sessionId, ulong requestId, string scheduledName, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterBackendCore.ScheduleMulti sessionId={sessionId}, requestId={requestId}, scheduledName={scheduledName}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(59, sessionId, requestId, scheduledName, elapsedMilliseconds);
            }
        }

        [Event(60, Level = EventLevel.Informational, Version = 1)]
        public void ExecuteScheduledCommandStarted(string scheduledName, string scheduledCommandPath, string faultPath)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterBackendCore.ExecuteScheduledCommand-Started scheduledName={scheduledName}, scheduledCommandPath={scheduledCommandPath}, faultPath={faultPath}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(60, scheduledName, scheduledCommandPath, faultPath);
            }
        }

        [Event(61, Level = EventLevel.Informational, Version = 1)]
        public void ExecuteScheduledCommandCompleted(string scheduledName, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"RingMasterBackendCore.ExecuteScheduledCommand-Completed scheduledName={scheduledName}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(61, scheduledName, elapsedMilliseconds);
            }
        }

        [Event(62, Level = EventLevel.Error, Version = 1)]
        public void ExecuteScheduledCommandFailed(string scheduledName, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceInformation($"RingMasterBackendCore.ExecuteScheduledCommand-Failed scheduledName={scheduledName}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(62, scheduledName, elapsedMilliseconds);
            }
        }

        [Event(63, Level = EventLevel.Error, Version = 1)]
        public void ExecuteScheduledCommandException(string scheduledName, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceInformation($"RingMasterBackendCore.ExecuteScheduledCommand-Exception scheduledName={scheduledName}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(63, scheduledName, exception);
            }
        }

        [Event(64, Level = EventLevel.Informational, Version = 1)]
        public void Multi(ulong requestId, int requestCount, bool completeSynchronously)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(64, requestId, requestCount, completeSynchronously);
            }
        }

        [Event(65, Level = EventLevel.Error, Version = 1)]
        public void MultiTimeout(ulong requestId, int requestCount, int requestsCompleted, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceWarning($"RingMasterBackendCore.Multi-Timeout requestId={requestId}, requestCount={requestCount}, requestsCompleted={requestsCompleted}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(65, requestId, requestCount, requestsCompleted, elapsedMilliseconds);
            }
        }

        [Event(66, Level = EventLevel.Informational, Version = 1)]
        public void Batch(ulong requestId, int requestCount, bool completeSynchronously)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(66, requestId, requestCount, completeSynchronously);
            }
        }

        [Event(67, Level = EventLevel.Error, Version = 1)]
        public void BatchTimeout(ulong requestId, int requestCount, int requestsCompleted, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceWarning($"RingMasterBackendCore.Batch-Timeout requestId={requestId}, requestCount={requestCount}, requestsCompleted={requestsCompleted}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(67, requestId, requestCount, requestsCompleted, elapsedMilliseconds);
            }
        }
    }
}
