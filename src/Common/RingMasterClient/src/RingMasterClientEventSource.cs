// <copyright file="RingMasterClientEventSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-RingMasterClient")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class RingMasterClientEventSource : EventSource
    {
        static RingMasterClientEventSource()
        {
        }

        private RingMasterClientEventSource()
        {
        }

        public static RingMasterClientEventSource Log { get; } = new RingMasterClientEventSource();

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        public void Create(string path)
        {
            this.WriteEvent(2, path);
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void Delete(string path, int version, bool isRecursive)
        {
            this.WriteEvent(3, path, version, isRecursive);
        }

        [Event(4, Level = EventLevel.Informational, Version = 1)]
        public void Exists(string path, bool watch)
        {
            this.WriteEvent(4, path, watch);
        }

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void GetChildren(string path, bool watch, string retrievalCondition)
        {
            this.WriteEvent(5, path, watch, retrievalCondition);
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void GetData(string path, bool watch)
        {
            this.WriteEvent(6, path, watch);
        }

        [Event(7, Level = EventLevel.Informational, Version = 1)]
        public void SetData(string path,  int version)
        {
            this.WriteEvent(7, path, version);
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void GetAcl(string path)
        {
            this.WriteEvent(8, path);
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void SetAcl(string path, int version)
        {
            this.WriteEvent(9, path, version);
        }

        [Event(10, Level = EventLevel.Informational, Version = 1)]
        public void Sync(string path)
        {
            this.WriteEvent(10, path);
        }

        [Event(11, Level = EventLevel.Informational, Version = 1)]
        public void Multi(int operationCount, bool mustCompleteSynchronously)
        {
            this.WriteEvent(11, operationCount, mustCompleteSynchronously);
        }

        [Event(12, Level = EventLevel.Informational, Version = 1)]
        public void Batch(int operationCount, bool mustCompleteSynchronously)
        {
            this.WriteEvent(12, operationCount, mustCompleteSynchronously);
        }

        [Event(13, Level = EventLevel.Informational, Version = 2)]
        public void Init(ulong requestId, ulong sessionId, string sessionPassword)
        {
            this.WriteEvent(13, requestId, sessionId, sessionPassword);
        }

        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void Close()
        {
            this.WriteEvent(14);
        }

        [Event(16, Level = EventLevel.Informational, Version = 1)]
        public void CompleteWithException(string exception)
        {
            this.WriteEvent(16, exception);
        }

        [Event(17, Level = EventLevel.Verbose, Version = 2)]
        public void Send(ulong connectionId, ulong callId, int packetLength)
        {
            this.WriteEvent(17, connectionId, callId, packetLength);
        }

        [Event(19, Level = EventLevel.Informational, Version = 2)]
        public void OnNewConnectionInitialized(ulong connectionId, string remoteEndpoint, string remoteIdentity)
        {
            this.WriteEvent(19, connectionId, remoteEndpoint, remoteIdentity);
        }

        [Event(20, Level = EventLevel.Informational, Version = 3)]
        public void ConnectionLost(ulong connectionId, string remoteEndpoint)
        {
            this.WriteEvent(20, connectionId, remoteEndpoint);
        }

        [Event(21, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessResponse(ulong callId, string responsePath, int resultCode)
        {
            this.WriteEvent(21, callId, responsePath ?? "<null>", resultCode);
        }

        [Event(22, Level = EventLevel.Verbose, Version = 2)]
        public void UnexpectedResponse(ulong callId, string remoteEndpoint)
        {
            this.WriteEvent(22, callId, remoteEndpoint);
        }

        [Event(23, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessageToClientFailedNotWatcherCall()
        {
            this.WriteEvent(23);
        }

        [Event(24, Level = EventLevel.Informational, Version = 1)]
        public void ProcessMessageToClientFailedWatcherDoesNotExist(ulong watcherId)
        {
            this.WriteEvent(24, watcherId);
        }

        [Event(25, Level = EventLevel.Informational, Version = 2)]
        public void DrainWatcher(ulong watcherId, string path)
        {
            this.WriteEvent(25, watcherId, path);
        }

        [Event(26, Level = EventLevel.Verbose, Version = 2)]
        public void OnPacketReceived(ulong connectionId, int packetLength)
        {
            this.WriteEvent(26, connectionId, packetLength);
        }

        [Event(27, Level = EventLevel.Error, Version = 3)]
        public void OnPacketReceivedFailed(ulong connectionId, string exception, string remoteEndpoint)
        {
            this.WriteEvent(27, connectionId, exception, remoteEndpoint);
        }

        [Event(28, Level = EventLevel.Informational, Version = 2)]
        public void RegisterWatcher(ulong watcherId, string path)
        {
            this.WriteEvent(28, watcherId, path);
        }

        [Event(30, Level = EventLevel.Informational, Version = 2)]
        public void NotifyConnectionLoss(ulong callId, string remoteEndpoint)
        {
            this.WriteEvent(30, callId, remoteEndpoint);
        }

        [Event(32, Level = EventLevel.Verbose, Version = 3)]
        public void NotifyResponseTimeout(ulong callId, long elapsedMilliseconds, string remoteEndpoint)
        {
            this.WriteEvent(32, callId, elapsedMilliseconds, remoteEndpoint);
        }

        [Event(35, Level = EventLevel.Informational, Version = 2)]
        public void Start(string endpoint)
        {
            this.WriteEvent(35, endpoint);
        }

        [Event(37, Level = EventLevel.Informational, Version = 2)]
        public void SendHeartbeat(ulong heartBeatCount)
        {
            this.WriteEvent(37, heartBeatCount);
        }

        [Event(38, Level = EventLevel.Error, Version = 1)]
        public void SendHeartbeatFailed(ulong heartBeatCount, string exception)
        {
            this.WriteEvent(38, heartBeatCount, exception);
        }

        [Event(39, Level = EventLevel.Error, Version = 2)]
        public void HeartbeatFailure(ulong connectionId, string remoteEndpoint)
        {
            this.WriteEvent(39, connectionId, remoteEndpoint);
        }

        [Event(40, Level = EventLevel.Informational, Version = 1)]
        public void Move(string pathSrc, string pathDst)
        {
            this.WriteEvent(40, pathSrc, pathDst);
        }

        [Event(41, Level = EventLevel.Informational, Version = 4)]
        public void RequestQueueFull(ulong requestId, int queueLength, string remoteEndpoint)
        {
            this.WriteEvent(41, requestId, queueLength, remoteEndpoint);
        }

        [Event(42, Level = EventLevel.Error, Version = 2)]
        public void RequestSendFailed(ulong connectionId, ulong requestId, string exception, string remoteEndpoint)
        {
            this.WriteEvent(42, connectionId, requestId, exception, remoteEndpoint);
        }

        [Event(43, Level = EventLevel.Informational, Version = 2)]
        public void ManageConnectionLifetimeTaskCanceled(ulong connectionId, string remoteEndpoint)
        {
            this.WriteEvent(43, connectionId, remoteEndpoint);
        }

        [Event(44, Level = EventLevel.Error, Version = 2)]
        public void ManageConnectionLifetimeTaskFailed(ulong connectionId, string exception, string remoteEndpoint)
        {
            this.WriteEvent(44, connectionId, exception, remoteEndpoint);
        }

        [Event(45, Level = EventLevel.Informational, Version = 1)]
        public void ManageConnectionLifetimeTaskCompleted(ulong connectionId, string remoteEndpoint, string remoteIdentity)
        {
            this.WriteEvent(45, connectionId, remoteEndpoint, remoteIdentity);
        }

        [Event(46, Level = EventLevel.Informational, Version = 2)]
        public void ManageRequestLifetimeTaskCanceled(string remoteEndpoint)
        {
            this.WriteEvent(46, remoteEndpoint);
        }

        [Event(48, Level = EventLevel.Informational, Version = 2)]
        public void ManageRequestLifetimeTaskCompleted(string remoteEndpoint)
        {
            this.WriteEvent(48, remoteEndpoint);
        }

        [Event(49, Level = EventLevel.Informational, Version = 1)]
        public void CloseRequestHandler()
        {
            this.WriteEvent(49);
        }

        [Event(50, Level = EventLevel.Informational, Version = 1)]
        public void DisposeRequestHandler()
        {
            this.WriteEvent(50);
        }

        [Event(51, Level = EventLevel.Informational, Version = 1)]
        public void DrainPendingRequests(int pendingRequestCount)
        {
            this.WriteEvent(51, pendingRequestCount);
        }

        [Event(52, Level = EventLevel.Informational, Version = 1)]
        public void DrainWatchers(int watcherCount)
        {
            this.WriteEvent(52, watcherCount);
        }

        [Event(53, Level = EventLevel.Informational, Version = 2)]
        public void ManageResponsesTaskCompleted(string remoteEndpoint)
        {
            this.WriteEvent(53, remoteEndpoint);
        }

        [Event(54, Level = EventLevel.Error, Version = 2)]
        public void ManageResponsesTaskFailed(string exception, string remoteEndpoint)
        {
            this.WriteEvent(54, exception, remoteEndpoint);
        }

        [Event(55, Level = EventLevel.Error, Version = 2)]
        public void ProcessResponseFailed(ulong callId, string exception, string remoteEndpoint)
        {
            this.WriteEvent(55, callId, exception, remoteEndpoint);
        }

        [Event(56, Level = EventLevel.Informational)]
        public void UnregisterWatcher(ulong watcherId)
        {
            this.WriteEvent(56, watcherId);
        }

        [Event(57, Level = EventLevel.Verbose)]
        public void EnableDispatch(ulong watcherId, string path)
        {
            this.WriteEvent(57, watcherId, path);
        }

        [Event(58, Level = EventLevel.Verbose)]
        public void DispatchWatcherNotification(ulong watcherId, string path, int eventType)
        {
            this.WriteEvent(58, watcherId, path, eventType);
        }

        [Event(59, Level = EventLevel.Warning)]
        public void RequestNoProcessTimeLeft(ulong callId, ushort requestType, string path)
        {
            this.WriteEvent(59, callId, requestType, path);
        }

        [Event(60, Level =EventLevel.Error)]
        public void DispatchWatcherEventQueueEmpty(ulong watcherId, string path)
        {
            this.WriteEvent(60, watcherId, path);
        }

        [Event(61, Level = EventLevel.Error)]
        public void DispatchWatcherFailed(ulong watcherId, string path, string exception)
        {
            this.WriteEvent(61, watcherId, path, exception);
        }
    }
}
