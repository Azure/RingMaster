// <copyright file="ZooKeeperServerEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-RingMaster-ZooKeeperServer")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class ZooKeeperServerEventSource : EventSource
    {
        private static readonly ZooKeeperServerEventSource LogInstance = new ZooKeeperServerEventSource();

        public ZooKeeperServerEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static ZooKeeperServerEventSource Log
        {
            get { return LogInstance; }
        }

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        public void RegisterTransport()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("ZooKeeperServer.RegisterTransport");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(2);
            }
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void Disposed()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("ZooKeeperServer.Disposed");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(3);
            }
        }

        [Event(4, Level = EventLevel.Informational, Version = 1)]
        public void SessionCreated(ulong sessionId, ulong connectionId, string client)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ZooKeeperServer.SessionCreated sessionId={sessionId}, connectionId={connectionId}, client={client}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(4, sessionId, connectionId, client);
            }
        }

        [Event(5, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessRequestCompleted(ulong sessionId, ulong callId, long elapsedMilliseconds)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ZooKeeperServer.ProcessRequest-Completed sessionId={sessionId}, callId={callId}, elapsedMilliseconds={elapsedMilliseconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(5, sessionId, callId, elapsedMilliseconds);
            }
        }

        [Event(6, Level = EventLevel.Error, Version = 1)]
        public void ProcessRequestFailed(ulong sessionId, ulong callId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"ZooKeeperServer.ProcessRequest-Failed sessionId={sessionId}, callId={callId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(6, sessionId, callId, exception);
            }
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void OnPacketReceived_Failed(ulong sessionId, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"ZooKeeperServer.OnPacketReceived-Failed sessionId={sessionId}, exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(7, sessionId, exception);
            }
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void SessionClosed(ulong sessionId, ulong connectionId, string client)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ZooKeeperServer.SessionClosed sessionId={sessionId}, connectionId={connectionId}, client={client}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(8, sessionId, connectionId, client);
            }
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void MakeWatcher(ulong sessionId, ulong watcherId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ZooKeeperServer.Session.MakeWatcher sessionId={sessionId}, watcherId={watcherId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(9, sessionId, watcherId);
            }
        }

        [Event(10, Level = EventLevel.Informational, Version = 1)]
        public void SendWatcherNotification(ulong sessionId, ulong watcherId)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ZooKeeperServer.Session.SendWatcherNotification sessionId={sessionId}, watcherId={watcherId}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(10, sessionId, watcherId);
            }
        }

        [Event(11, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessRequest(ulong sessionId, ulong callId, int requestType, string path, int packetLength, uint protocolVersion, string sessionState)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ZooKeeperServer.ProcessRequest sessionId={sessionId}, callId={callId}, requestType={requestType}, path={path}, packetLength={packetLength}, protocolVersion={protocolVersion}, sessionState={sessionState}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11, sessionId, callId, requestType, path, packetLength, protocolVersion, sessionState);
            }
        }

        [Event(12, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessSessionInit(ulong sessionId, int resultCode)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ZooKeeperServer.ProcessSessionInit sessionId={sessionId}, resultCode={resultCode}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(12, sessionId, resultCode);
            }
        }

        [Event(13, Level = EventLevel.Informational, Version = 1)]
        public void RedirectionSuggested(ulong sessionId, string suggestedConnectionString)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ZooKeeperServer.RedirectionSuggested sessionId={sessionId}, suggestedConnectionString={suggestedConnectionString}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(13, sessionId, suggestedConnectionString);
            }
        }

        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void NullResponsePacket(ulong callid, string protocolRequest, string ringMasterRequest)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ResponsePacket was null for response.Callid={callid}, ZkprRequest={protocolRequest}, RingMasterRequest={ringMasterRequest}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(14, callid, protocolRequest, ringMasterRequest);
            }
        }

        [Event(15, Level = EventLevel.Informational, Version = 1)]
        public void ProcessRequestSucceeded(ulong sessionId, ulong callId, int requestType, string path, int packetLength, uint protocolVersion, string sessionState)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ZooKeeperServer.ProcessRequestSucceeded sessionId={sessionId}, callId={callId}, requestType={requestType}, path={path}, packetLength={packetLength}, protocolVersion={protocolVersion}, sessionState={sessionState}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11, sessionId, callId, requestType, path, packetLength, protocolVersion, sessionState);
            }
        }
    }
}