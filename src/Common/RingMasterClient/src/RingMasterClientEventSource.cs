// <copyright file="RingMasterClientEventSource.cs" company="Microsoft">
//     Copyright ©  2015
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
        private static readonly RingMasterClientEventSource LogInstance = new RingMasterClientEventSource();

        public RingMasterClientEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static RingMasterClientEventSource Log
        {
            get { return LogInstance; }
        }

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        public void Create(string path)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.Create path={0}", path);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(2, path);
            }
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void Delete(string path, int version, bool isRecursive)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.Delete path={0}, version={1}, isRecursive={2}", path, version, isRecursive);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(3, path, version, isRecursive);
            }
        }

        [Event(4, Level = EventLevel.Informational, Version = 1)]
        public void Exists(string path, bool watch)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.Exists path={0}, watch={1}", path, watch);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(4, path, watch);
            }
        }

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void GetChildren(string path, bool watch, string retrievalCondition)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.GetChildren path={0}, watch={1}, retrievalCondition={2}", path, watch, retrievalCondition);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(5, path, watch, retrievalCondition);
            }
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void GetData(string path, bool watch)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.GetData path={0}, watch={1}", path, watch);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(6, path, watch);
            }
        }

        [Event(7, Level = EventLevel.Informational, Version = 1)]
        public void SetData(string path,  int version)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.SetData path={0}, version={1}", path, version);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(7, path, version);
            }
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void GetAcl(string path)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.GetAcl path={0}", path);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(8, path);
            }
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void SetAcl(string path, int version)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.SetAcl path={0}, version={1}", path, version);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(9, path, version);
            }
        }

        [Event(10, Level = EventLevel.Informational, Version = 1)]
        public void Sync(string path)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.Sync path={0}", path);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(10, path);
            }
        }

        [Event(11, Level = EventLevel.Informational, Version = 1)]
        public void Multi(int operationCount, bool mustCompleteSynchronously)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.Multi operationCount={0}, mustCompleteSynchronously={1}", operationCount, mustCompleteSynchronously);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11, operationCount, mustCompleteSynchronously);
            }
        }

        [Event(12, Level = EventLevel.Informational, Version = 1)]
        public void Batch(int operationCount, bool mustCompleteSynchronously)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.Batch operationCount={0}, mustCompleteSynchronously={1}", operationCount, mustCompleteSynchronously);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(12, operationCount, mustCompleteSynchronously);
            }
        }

        [Event(13, Level = EventLevel.Informational, Version = 2)]
        public void Init(ulong requestId, ulong sessionId, string sessionPassword)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterClient.Init requestId={requestId}, sessionId={sessionId}, sessionPassword={sessionPassword}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(13, requestId, sessionId, sessionPassword);
            }
        }

        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void Close()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation("RingMasterClient.Close");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(14);
            }
        }

        [Event(16, Level = EventLevel.Informational, Version = 1)]
        public void CompleteWithException(string exception)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceError("RingMasterClient.CompleteWithException exception={0}", exception);
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(16, exception);
            }
        }

        [Event(17, Level = EventLevel.Verbose, Version = 2)]
        public void Send(ulong connectionId, ulong callId, int packetLength)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.Send connectionId={connectionId}, callId={callId}, packetLength={packetLength}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(17, connectionId, callId, packetLength);
            }
        }

        [Event(19, Level = EventLevel.Informational, Version = 2)]
        public void OnNewConnectionInitialized(ulong connectionId, string remoteEndpoint, string remoteIdentity)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.OnNewConnectionInitialized connectionId={connectionId}, remoteEndpoint={remoteEndpoint}, remoteIdentity={remoteIdentity}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(19, connectionId, remoteEndpoint, remoteIdentity);
            }
        }

        [Event(20, Level = EventLevel.Informational, Version = 2)]
        public void ConnectionLost(ulong connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceError($"RingMasterRequestHandler.ConnectionLost connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(20, connectionId);
            }
        }

        [Event(21, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessResponse(ulong callId, string responsePath, int resultCode)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.ProcessResponse callId={callId}, responsePath={responsePath}, resultCode={resultCode}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(21, callId, responsePath ?? "<null>", resultCode);
            }
        }

        [Event(22, Level = EventLevel.Verbose, Version = 1)]
        public void UnexpectedResponse(ulong callId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.UnexpectedResponse callId={callId}. This callId may have already been processed (e.g. timed out).");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(22, callId);
            }
        }

        [Event(23, Level = EventLevel.Error, Version = 1)]
        public void ProcessMessageToClientFailedNotWatcherCall()
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.ProcessMessageToClient - Not a watcher call");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(23);
            }
        }

        [Event(24, Level = EventLevel.Informational, Version = 1)]
        public void ProcessMessageToClientFailedWatcherDoesNotExist(ulong watcherId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.ProcessMessageToClient - Watcher does not exist watcherId={watcherId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(24, watcherId);
            }
        }

        [Event(25, Level = EventLevel.Informational, Version = 2)]
        public void DrainWatcher(ulong watcherId, string path)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.DrainWatcher watcherId={watcherId}, path={path}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(25, watcherId, path);
            }
        }

        [Event(26, Level = EventLevel.Verbose, Version = 2)]
        public void OnPacketReceived(ulong connectionId, int packetLength)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.OnPacketReceived connectionId={connectionId}, packetLength={packetLength}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(26, connectionId, packetLength);
            }
        }

        [Event(27, Level = EventLevel.Error, Version = 2)]
        public void OnPacketReceivedFailed(ulong connectionId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.OnPacketReceivedFailed connectionId={connectionId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(27, connectionId, exception);
            }
        }

        [Event(28, Level = EventLevel.Informational, Version = 2)]
        public void RegisterWatcher(ulong watcherId, string path)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.RegisterWatcher watcherId={watcherId}, path={path}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(28, watcherId, path);
            }
        }

        [Event(30, Level = EventLevel.Informational, Version = 1)]
        public void NotifyConnectionLoss(ulong callId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.NotifyConnectionLoss callId={callId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(30, callId);
            }
        }

        [Event(32, Level = EventLevel.Verbose, Version = 2)]
        public void NotifyResponseTimeout(ulong callId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.NotifyResponseTimeout callId={callId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(32, callId, elapsedMilliseconds);
            }
        }

        [Event(35, Level = EventLevel.Informational, Version = 2)]
        public void Start(string endpoint)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterClient.Start endpoint={endpoint}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(35, endpoint);
            }
        }

        [Event(36, Level = EventLevel.Informational, Version = 1)]
        public void GetDataWithStat(string path, bool watch)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterClient.GetDataWithStat path={path}, watch={watch}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(36, path, watch);
            }
        }

        [Event(37, Level = EventLevel.Informational, Version = 2)]
        public void SendHeartbeat(ulong heartBeatCount)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.SendHeartbeat heartBeatCount={heartBeatCount}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(37, heartBeatCount);
            }
        }

        [Event(38, Level = EventLevel.Error, Version = 1)]
        public void SendHeartbeatFailed(ulong heartBeatCount, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.SendHeartbeat-Failed heartBeatCount={heartBeatCount}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(38, heartBeatCount, exception);
            }
        }

        [Event(39, Level = EventLevel.Error, Version = 1)]
        public void HeartbeatFailure(ulong connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.HeartbeatFailure-Terminating connection connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(39);
            }
        }

        [Event(40, Level = EventLevel.Informational, Version = 1)]
        public void Move(string pathSrc, string pathDst)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterClient.Move pathSrc={pathSrc} pathDst={pathDst}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(40, pathSrc, pathDst);
            }
        }

        [Event(41, Level = EventLevel.Informational, Version = 3)]
        public void RequestQueueFull(ulong requestId, int queueLength)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.RequestQueueFull requestId={requestId}, queueLength={queueLength}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(41, requestId, queueLength);
            }
        }

        [Event(42, Level = EventLevel.Error, Version = 1)]
        public void RequestSendFailed(ulong connectionId, ulong requestId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.RequestSendFailed connectionId={connectionId}, requestId={requestId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(42, connectionId, requestId, exception);
            }
        }

        [Event(43, Level = EventLevel.Informational, Version = 1)]
        public void ManageConnectionLifetimeTaskCanceled(ulong connectionId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.ManageConnectionLifetimeTask-Canceled connectionId={connectionId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(43, connectionId);
            }
        }

        [Event(44, Level = EventLevel.Error, Version = 1)]
        public void ManageConnectionLifetimeTaskFailed(ulong connectionId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.ManageConnectionLifetimeTask-Failed connectionId={connectionId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(44, connectionId, exception);
            }
        }

        [Event(45, Level = EventLevel.Informational, Version = 1)]
        public void ManageConnectionLifetimeTaskCompleted(ulong connectionId, string remoteEndpoint, string remoteIdentity)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.ManageConnectionLifetimeTask-Completed connectionId={connectionId}, remoteEndpoint={remoteEndpoint}, remoteIdentity={remoteIdentity}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(45, connectionId, remoteEndpoint, remoteIdentity);
            }
        }

        [Event(46, Level = EventLevel.Informational, Version = 1)]
        public void ManageRequestLifetimeTaskCanceled()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.ManageRequestLifetimeTask-Canceled");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(46);
            }
        }

        [Event(48, Level = EventLevel.Informational, Version = 1)]
        public void ManageRequestLifetimeTaskCompleted()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.ManageRequestLifetimeTask-Completed");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(48);
            }
        }

        [Event(49, Level = EventLevel.Informational, Version = 1)]
        public void CloseRequestHandler()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.Close");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(49);
            }
        }

        [Event(50, Level = EventLevel.Informational, Version = 1)]
        public void DisposeRequestHandler()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.Dispose");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(50);
            }
        }

        [Event(51, Level = EventLevel.Informational, Version = 1)]
        public void DrainPendingRequests(int pendingRequestCount)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.DrainPendingRequests pendingRequestCount={pendingRequestCount}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(51, pendingRequestCount);
            }
        }

        [Event(52, Level = EventLevel.Informational, Version = 1)]
        public void DrainWatchers(int watcherCount)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.DrainWatchers watcherCount={watcherCount}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(52, watcherCount);
            }
        }

        [Event(53, Level = EventLevel.Informational, Version = 1)]
        public void ManageResponsesTaskCompleted()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.ManageResponsesTask-Completed");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(53);
            }
        }

        [Event(54, Level = EventLevel.Error, Version = 1)]
        public void ManageResponsesTaskFailed(string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.ManageResponsesTask-Failed exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(54, exception);
            }
        }

        [Event(55, Level = EventLevel.Error, Version = 1)]
        public void ProcessResponseFailed(ulong callId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"RingMasterRequestHandler.ProcessResponseFailed callId={callId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(55, callId, exception);
            }
        }

        [Event(56, Level = EventLevel.Informational)]
        public void UnregisterWatcher(ulong watcherId)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.UnregisterWatcher watcherId={watcherId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(56, watcherId);
            }
        }

        [Event(57, Level = EventLevel.Verbose)]
        public void EnableDispatch(ulong watcherId, string path)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.EnableDispatch watcherId={watcherId}, path={path}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(57, watcherId, path);
            }
        }

        [Event(58, Level = EventLevel.Verbose)]
        public void DispatchWatcherNotification(ulong watcherId, string path, int eventType)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"RingMasterRequestHandler.DispatchWatcherNotification watcherId={watcherId}, path={path}, eventType={eventType}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(58, watcherId, path, eventType);
            }
        }
    }
}