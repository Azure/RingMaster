// <copyright file="RingMasterEventSource.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Diagnostics.Tracing;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// RingMaster events
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Backend-RingMasterEvents")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class RingMasterEventSource : EventSource
    {
        private const int ArgsLength = 7;
        private static readonly ObjectPool<object[]> ArgsPool = new ObjectPool<object[]>(() => new object[ArgsLength], o => o, size: Environment.ProcessorCount);

        static RingMasterEventSource()
        {
        }

        private RingMasterEventSource()
        {
        }

        public static RingMasterEventSource Log { get; } = new RingMasterEventSource();

        [Event(2, Level = EventLevel.Informational, Version = 2)]
        public void ApplyBatchSucceeded(ulong timeStreamId, ulong batchId, ulong minTransactionId, ulong maxTransactionId, long elapsedMilliseconds)
        {
            this.WriteEvent(2, timeStreamId, batchId, minTransactionId, maxTransactionId, elapsedMilliseconds);
        }

        [Event(3, Level = EventLevel.Error, Version = 2)]
        public void ApplyBatch_ExternalTransactionIdNotInSequence(ulong timeStreamId, ulong batchId, ulong transactionId, ulong lastExternallyAssignedTransactionId)
        {
            this.WriteEvent(3, timeStreamId, batchId, transactionId, lastExternallyAssignedTransactionId);
        }

        [Event(4, Level = EventLevel.Error, Version = 2)]
        public void ApplyBatch_BatchIdNotInSequence(ulong timeStreamId, ulong batchId, ulong lastAppliedBatchId)
        {
            this.WriteEvent(4, timeStreamId, batchId, lastAppliedBatchId);
        }

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested(bool poisonPillPrevented, bool replicationIsInReadOnly)
        {
            this.WriteEvent(5, poisonPillPrevented, replicationIsInReadOnly);
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested_FinalSet(string path)
        {
            this.WriteEvent(6, path);
        }

        [Event(7, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested_FullLockdown()
        {
            this.WriteEvent(7);
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void OnPathsLockdownRequested_PartialLockdown(string path)
        {
            this.WriteEvent(8, path);
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void PathIsLockedDown(string path)
        {
            this.WriteEvent(9, path);
        }

        [Event(10, Level = EventLevel.Warning, Version = 2)]
        public void ApplyBatchAborted(ulong timeStreamId, ulong batchId, ulong minTransactionId, ulong maxTransactionId, ulong abortedAtTransactionId, long elapsedMilliseconds)
        {
            this.WriteEvent(10, timeStreamId, batchId, minTransactionId, maxTransactionId, abortedAtTransactionId, elapsedMilliseconds);
        }

        [Event(11, Level = EventLevel.LogAlways, Version = 1)]
        public void InitializeSession(ulong sessionId, string clientIP, string clientIdentity)
        {
            this.WriteEvent(11, sessionId, clientIP ?? "<null>", clientIdentity ?? "<null>");
        }

        [Event(12, Level = EventLevel.LogAlways, Version = 1)]
        public void SetAuth(ulong sessionId, string clientIP, string clientIdentity, string clientDigest)
        {
            this.WriteEvent(12, sessionId, clientIP, clientIdentity, clientDigest);
        }

        [Event(13, Level = EventLevel.LogAlways, Version = 1)]
        public void RequestCommand(ulong sessionId, string clientIP, string clientIdentity, string path)
        {
            this.WriteEvent(13, sessionId, clientIP, clientIdentity, path);
        }

        [Event(14, Level = EventLevel.LogAlways, Version = 1)]
        public void RunCommand(string command)
        {
            this.WriteEvent(14, command);
        }

        [Event(15, Level = EventLevel.Error, Version = 2)]
        public void ProcessMessageFailed(ulong sessionId, ulong requestId, long zxid, int requestType, string path, int retryCount, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(15, sessionId, requestId, zxid, requestType, path, retryCount, elapsedMilliseconds, exception);
        }

        [Event(16, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_PersistedDataFactoryNotActive(ulong sessionId, ulong requestId)
        {
            this.WriteEvent(16, sessionId, requestId);
        }

        [Event(17, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_RequestValidationFailed(ulong sessionId, ulong requestId)
        {
            this.WriteEvent(17, sessionId, requestId);
        }

        [Event(18, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_FailedToAcquireExecutionQueue(ulong sessionId, ulong requestId, string executionQueueId)
        {
            this.WriteEvent(18, sessionId, requestId, executionQueueId);
        }

        /// <summary>
        /// Trace that the message is processed successfully.
        /// </summary>
        /// <remarks>
        /// This method allows avoiding still quite expensive string computation if the event is disabled.
        /// Plus, overall, this method is called billions of times a day an might be off by default.
        /// </remarks>
        [NonEvent]
        public void ProcessMessageSucceeded(ulong sessionId, ulong requestId, long zxid, int requestType, string path, long elapsedMilliseconds, RequestResponse response)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.ProcessMessage))
            {
                this.ProcessMessageSucceeded(sessionId, requestId, zxid, requestType, path, elapsedMilliseconds, response.ToStringFast());
            }
        }

        [Event(19, Version = 3, Keywords = Keywords.ProcessMessage)]
        public void ProcessMessageSucceeded(ulong sessionId, ulong requestId, long zxid, int requestType, string path, long elapsedMilliseconds, string response)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.ProcessMessage))
            {
                // This method is called a lot (like 10B times in a day), so the performance matters a lot.
                // Based on various performance analysis, we decided to pool the object array instead of using
                // "unsafe" implementation based on stack allocated 'EventData' followed by 'this.WriteEventCore' call.
                // Even though this method boxes all the arguments, it's still faster than using 'EventData',
                // because 'WriteEventCore' will copy all the strings arguments in DecodeObjects.
                // Here is the benchmark results for 3 options:
                // | Method                                                   | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
                // |--------------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
                // | ProcessMessageSucceeded_OldToString_WriteEvent           | 1,022.0 ns | 19.77 ns | 20.31 ns |  1.00 |    0.00 | 0.0687 |    1152 B |        1.00 |
                // | ProcessMessageSucceeded_New_ToString_WriteEventCore      |   510.0 ns | 10.08 ns | 12.38 ns |  0.50 |    0.02 | 0.0467 |     784 B |        0.68 |
                // | ProcessMessageSucceeded_NewToString_WriteEvent_WithCache |   477.6 ns |  2.55 ns |  2.39 ns |  0.47 |    0.01 | 0.0310 |     520 B |        0.45 |
                // The first one is the old version, the second one uses 'EventData' and calls 'WriteEventCore'.
                // Keeping the last one since it allocates way less than the version that uses 'WriteEventCore'.
                var args = ArgsPool.Rent();

                args[0] = sessionId;
                args[1] = requestId;
                args[2] = zxid;
                args[3] = requestType;
                args[4] = path;
                args[5] = elapsedMilliseconds;
                args[6] = response;
                this.WriteEvent(19, args);
                ArgsPool.PutInstance(args);
            }
        }

        [Event(20, Level = EventLevel.Verbose, Version = 1)]
        public void RequestCreateSucceeded(ulong sessionId, ulong requestId, string path, long elapsedMilliseconds)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(20, sessionId, requestId, path, elapsedMilliseconds);
            }
        }

        [Event(21, Level = EventLevel.Verbose, Version = 1)]
        public void RequestGetChildrenSucceeded(ulong sessionId, ulong requestId, string path, string retrievalCondition, int childrenCount, long elapsedMilliseconds)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(21, sessionId, requestId, path, retrievalCondition, childrenCount, elapsedMilliseconds);
            }
        }

        [Event(22, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenEnumerationLimitExceeded(ulong sessionId, ulong requestId, string path, int childrenCount, int enumerationLimit)
        {
            this.WriteEvent(22, sessionId, requestId, path, childrenCount, enumerationLimit);
        }

        [Event(23, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenRetrieveChildrenFailed(ulong sessionId, ulong requestId, string path, int childrenCount, string retrievalCondition, string exception)
        {
            this.WriteEvent(23, sessionId, requestId, path, childrenCount, retrievalCondition, exception);
        }

        [Event(24, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenInvalidRetrievalCondition(string retrievalCondition)
        {
            this.WriteEvent(24, retrievalCondition);
        }

        [Event(25, Level = EventLevel.Informational, Version = 1)]
        public void RingMasterNodeChildrenLimits(int enumerationLimit, int minSortedDictionaryThreshold, int maxSortedDictionaryThreshold)
        {
            this.WriteEvent(25, enumerationLimit, minSortedDictionaryThreshold, maxSortedDictionaryThreshold);
        }

        [Event(26, Level = EventLevel.Error, Version = 1)]
        public void RequestGetDataFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(26, sessionId, requestId, path, exception);
        }

        [Event(27, Level = EventLevel.Error, Version = 1)]
        public void RequestCreateFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(27, sessionId, requestId, path, exception);
        }

        [Event(28, Level = EventLevel.Error, Version = 1)]
        public void RequestGetChildrenFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(28, sessionId, requestId, path, exception);
        }

        [Event(29, Level = EventLevel.Error, Version = 1)]
        public void RequestExistsFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(29, sessionId, requestId, path, exception);
        }

        [Event(30, Level = EventLevel.Error, Version = 1)]
        public void RequestDeleteFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(30, sessionId, requestId, path, exception);
        }

        [Event(31, Level = EventLevel.Error, Version = 1)]
        public void RequestSetDataFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(31, sessionId, requestId, path, exception);
        }

        [Event(32, Level = EventLevel.Error, Version = 1)]
        public void RequestSetAclFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(32, sessionId, requestId, path, exception);
        }

        [Event(33, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherSet(ulong sessionId, string childId, string watcherContext)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(33, sessionId, childId, watcherContext);
            }
        }

        [Event(34, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherRemoved(ulong sessionId, string childId, string watcherContext)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(34, sessionId, childId, watcherContext);
            }
        }

        [Event(35, Level = EventLevel.Verbose, Version = 1)]
        public void TryRemoveOnTerminateAction(ulong sessionId, string actionName, bool wasRemoved)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(35, sessionId, actionName, wasRemoved);
            }
        }

        [Event(36, Level = EventLevel.Verbose, Version = 1)]
        public void RemoveOnTerminateAction(ulong sessionId, string actionName)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(36, sessionId, actionName);
            }
        }

        [Event(37, Level = EventLevel.Verbose, Version = 1)]
        public void AddOnTerminateAction(ulong sessionId, string actionName)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(37, sessionId, actionName);
            }
        }

        [Event(38, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_Timeout(ulong sessionId, ulong requestId, long elapsedMilliseconds)
        {
            this.WriteEvent(38, sessionId, requestId, elapsedMilliseconds);
        }

        [Event(39, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_Start(int threadCount)
        {
            this.WriteEvent(39, threadCount);
        }

        [Event(40, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_ProcessPendingRequestsThreadStarted(int threadId)
        {
            this.WriteEvent(40, threadId);
        }

        [Event(41, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_ProcessPendingRequestsThreadStopped(int threadId)
        {
            this.WriteEvent(41, threadId);
        }

        [Event(42, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_Stopping(int threadCount)
        {
            this.WriteEvent(42, threadCount);
        }

        [Event(43, Level = EventLevel.LogAlways, Version = 1)]
        public void Executor_Stopped()
        {
            this.WriteEvent(43);
        }

        [Event(44, Level = EventLevel.Error, Version = 1)]
        public void Executor_RequestQueueOverflow(long sequenceNumber, ulong sessionId, ulong requestId)
        {
            this.WriteEvent(44, sequenceNumber, sessionId, requestId);
        }

        [Event(45, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessRequestFailed(long sequenceNumber, string exception)
        {
            this.WriteEvent(45, sequenceNumber, exception);
        }

        [Event(46, Level = EventLevel.Verbose, Version = 1)]
        public void Executor_ProcessRequestCompleted(long sequenceNumber, long elapsedMilliseconds)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(46, sequenceNumber, elapsedMilliseconds);
            }
        }

        [Event(47, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessRequestCancelled(long sequenceNumber)
        {
            this.WriteEvent(47, sequenceNumber);
        }

        [Event(48, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessRequestTimedout(long sequenceNumber)
        {
            this.WriteEvent(48, sequenceNumber);
        }

        [Event(49, Level = EventLevel.Verbose, Version = 1)]
        public void Executor_RequestQueued(long sequenceNumber, ulong sessionId, ulong requestId)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(49, sequenceNumber, sessionId, requestId);
            }
        }

        [Event(50, Level = EventLevel.Error, Version = 1)]
        public void Executor_ProcessPendingRequestsThreadFailed(int threadId, string exception)
        {
            this.WriteEvent(50, threadId, exception);
        }

        [Event(51, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemoveExistingWatcher(ulong sessionId, string watchedPath)
        {
            this.WriteEvent(51, sessionId, watchedPath);
        }

        [Event(52, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_AddWatcher(ulong sessionId, string watchedPath, int watcherCount, long elapsedMilliseconds)
        {
            this.WriteEvent(52, sessionId, watchedPath, watcherCount, elapsedMilliseconds);
        }

        [Event(53, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemoveWatcher(ulong sessionId, string watchedPath, int watcherCount, long elapsedMilliseconds)
        {
            this.WriteEvent(53, sessionId, watchedPath, watcherCount, elapsedMilliseconds);
        }

        [Event(54, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherCollection_WatcherApplies(string path, ulong sessionId, string watchedPath)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(54, path, sessionId, watchedPath);
            }
        }

        [Event(55, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_EnumeratedApplicableWatchers(string path, int applicableWatchersCount, long elapsedMilliseconds)
        {
            this.WriteEvent(55, path, applicableWatchersCount, elapsedMilliseconds);
        }

        [Event(56, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemoveWatcherOnSessionTermination(ulong sessionId, string watchedPath, int watcherCount)
        {
            this.WriteEvent(56, sessionId, watchedPath, watcherCount);
        }

        [Event(57, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_CleanupOnSessionTermination(ulong sessionId, string watchedPath)
        {
            this.WriteEvent(57, sessionId, watchedPath);
        }

        [Event(58, Level = EventLevel.Informational, Version = 1)]
        public void WatcherCollection_RemovedAllWatchersForSession(ulong sessionId, int removedCount, int watcherCount, long elapsedMilliseconds)
        {
            this.WriteEvent(58, sessionId, removedCount, watcherCount, elapsedMilliseconds);
        }

        [Event(59, Level = EventLevel.Informational, Version = 1)]
        public void ScheduleMultiCompleted(ulong sessionId, ulong requestId, string scheduledName, long elapsedMilliseconds)
        {
            this.WriteEvent(59, sessionId, requestId, scheduledName, elapsedMilliseconds);
        }

        [Event(60, Level = EventLevel.Informational, Version = 1)]
        public void ExecuteScheduledCommandStarted(string scheduledName, string scheduledCommandPath, string faultPath)
        {
            this.WriteEvent(60, scheduledName, scheduledCommandPath, faultPath);
        }

        [Event(61, Level = EventLevel.Informational, Version = 1)]
        public void ExecuteScheduledCommandCompleted(string scheduledName, long elapsedMilliseconds)
        {
            this.WriteEvent(61, scheduledName, elapsedMilliseconds);
        }

        [Event(62, Level = EventLevel.Error, Version = 1)]
        public void ExecuteScheduledCommandFailed(string scheduledName, long elapsedMilliseconds)
        {
            this.WriteEvent(62, scheduledName, elapsedMilliseconds);
        }

        [Event(63, Level = EventLevel.Error, Version = 1)]
        public void ExecuteScheduledCommandException(string scheduledName, string exception)
        {
            this.WriteEvent(63, scheduledName, exception);
        }

        [Event(64, Level = EventLevel.Verbose, Version = 1)]
        public void Multi(ulong requestId, int requestCount, bool completeSynchronously)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(64, requestId, requestCount, completeSynchronously);
            }
        }

        [Event(65, Level = EventLevel.Error, Version = 1)]
        public void MultiTimeout(ulong requestId, int requestCount, int requestsCompleted, long elapsedMilliseconds)
        {
            this.WriteEvent(65, requestId, requestCount, requestsCompleted, elapsedMilliseconds);
        }

        [Event(66, Level = EventLevel.Verbose, Version = 1)]
        public void Batch(ulong requestId, int requestCount, bool completeSynchronously)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(66, requestId, requestCount, completeSynchronously);
            }
        }

        [Event(67, Level = EventLevel.Error, Version = 1)]
        public void BatchTimeout(ulong requestId, int requestCount, int requestsCompleted, long elapsedMilliseconds)
        {
            this.WriteEvent(67, requestId, requestCount, requestsCompleted, elapsedMilliseconds);
        }

        [Event(68, Level = EventLevel.Verbose, Version = 1)]
        public void LockCollectionAcquired(int threadId, string pathList)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(68, threadId, pathList);
            }
        }

        [Event(69, Level = EventLevel.Verbose, Version = 1)]
        public void LockCollectionReleased(int threadId)
        {
            if (this.IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                this.WriteEvent(69, threadId);
            }
        }

        [Event(70, Level = EventLevel.Error, Version = 1)]
        public void RequestGetSubtreeFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(70, sessionId, requestId, path, exception);
        }

        [Event(71, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessage_RootIsNull(ulong sessionId, ulong requestId)
        {
            this.WriteEvent(71, sessionId, requestId);
        }

        [Event(72, Level = EventLevel.Warning, Version = 1)]
        public void AcquireWriterLockFailed(double timeSpentMs, int nodeLevel, string nodeName)
        {
            this.WriteEvent(72, timeSpentMs, nodeLevel, nodeName);
        }

        [Event(73, Level = EventLevel.Error, Version = 1)]
        public void RequestSetDataAndUserMetadataFailed(ulong sessionId, ulong requestId, string path, string exception)
        {
            this.WriteEvent(73, sessionId, requestId, path, exception);
        }

        [Event(74, Level = EventLevel.Error, Version = 1)]
        public void InvokeCallbackBeforeCompleteFailed(int requestType, string path, string exception)
        {
            this.WriteEvent(74, requestType, path, exception);
        }

        [Event(75, Level = EventLevel.Warning, Version = 1)]
        public void IUnsafeTreeAccessAcquireWriteLockWarning(string message)
        {
            this.WriteEvent(75, message);
        }

        [Event(76, Level = EventLevel.Error, Version = 1)]
        public void IUnsafeTreeAccessAcquireWriteLockFailed(string message)
        {
            this.WriteEvent(76, message);
        }

        [Event(77, Level = EventLevel.Warning, Version = 1)]
        public void WatcherTerminateActionAcquireWriteLockWarning(ulong sessionId, string message)
        {
            this.WriteEvent(77, sessionId, message);
        }

        [Event(78, Level = EventLevel.Error, Version = 1)]
        public void WatcherTerminateActionAcquireWriteLockFailed(ulong sessionId, string message)
        {
            this.WriteEvent(78, sessionId, message);
        }

        /// <summary>
        /// A bit vector with keywords used by <see cref="RingMasterEventSource"/>.
        /// </summary>
        public static class Keywords
        {
            /// <summary>
            /// The keyword used by <see cref="ProcessMessageSucceeded(ulong,ulong,long,int,string,long,string)"/>.
            /// </summary>
            public const EventKeywords ProcessMessage = (EventKeywords)0x0001;
        }
    }
}
