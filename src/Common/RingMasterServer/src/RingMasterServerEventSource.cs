// <copyright file="RingMasterServerEventSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-RingMaster-RingMasterServer")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class RingMasterServerEventSource : EventSource
    {
        static RingMasterServerEventSource()
        {
        }

        private RingMasterServerEventSource()
        {
        }

        public static RingMasterServerEventSource Log { get; } = new RingMasterServerEventSource();

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        public void RegisterTransport()
        {
            this.WriteEvent(2);
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void Disposed()
        {
            this.WriteEvent(3);
        }

        [Event(4, Level = EventLevel.Informational, Version = 1)]
        public void SessionCreated(ulong sessionId, ulong connectionId, string client)
        {
            this.WriteEvent(4, sessionId, connectionId, client);
        }

        [Event(5, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessRequestCompleted(ulong sessionId, ulong callId, long elapsedMilliseconds)
        {
            this.WriteEvent(5, sessionId, callId, elapsedMilliseconds);
        }

        [Event(6, Level = EventLevel.Error, Version = 1)]
        public void ProcessRequestFailed(ulong sessionId, ulong callId, string exception)
        {
            this.WriteEvent(6, sessionId, callId, exception);
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void OnPacketReceived_Failed(ulong sessionId, string exception)
        {
            this.WriteEvent(7, sessionId, exception);
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void SessionClosed(ulong sessionId, ulong connectionId, string client)
        {
            this.WriteEvent(8, sessionId, connectionId, client);
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void MakeWatcher(ulong sessionId, ulong watcherId)
        {
            this.WriteEvent(9, sessionId, watcherId);
        }

        [Event(10, Level = EventLevel.Informational, Version = 1)]
        public void SendWatcherNotification(ulong sessionId, ulong watcherId)
        {
            this.WriteEvent(10, sessionId, watcherId);
        }

        [Event(11, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessRequest(ulong sessionId, ulong callId, int requestType, string path, int packetLength, uint protocolVersion)
        {
            this.WriteEvent(11, sessionId, callId, requestType, path, packetLength, protocolVersion);
        }

        [Event(12, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessSessionInit(ulong sessionId, int resultCode)
        {
            this.WriteEvent(12, sessionId, resultCode);
        }

        [Event(13, Level = EventLevel.Informational, Version = 1)]
        public void RedirectionSuggested(ulong sessionId, string suggestedConnectionString)
        {
            this.WriteEvent(13, sessionId, suggestedConnectionString);
        }
    }
}
