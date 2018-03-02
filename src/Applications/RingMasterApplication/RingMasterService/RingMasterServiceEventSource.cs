// <copyright file="RingMasterServiceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterService
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-RingMasterService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class RingMasterServiceEventSource : EventSource
    {
        public RingMasterServiceEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static RingMasterServiceEventSource Log { get; } = new RingMasterServiceEventSource();

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.LogAlways, Version = 1)]
        public void UnhandledException(string exception, bool isTerminating)
        {
            this.WriteEvent(2, exception, isTerminating);
        }

        [Event(3, Level = EventLevel.LogAlways, Version = 1)]
        public void RegisterServiceSucceeded()
        {
            this.WriteEvent(3);
        }

        [Event(4, Level = EventLevel.Error, Version = 1)]
        public void RegisterServiceFailed(string exception)
        {
           this.WriteEvent(4, exception);
        }

        [Event(5, Level = EventLevel.LogAlways, Version = 2)]
        public void ReportServiceStatus(string version, long uptimeInSeconds, int totalSessionCount)
        {
           this.WriteEvent(5, version, uptimeInSeconds, totalSessionCount);
        }

        [Event(6, Level = EventLevel.Error, Version = 1)]
        public void RunAsyncFailed(string exception)
        {
            this.WriteEvent(6, exception);
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void CreateListener_GetEndpointFailed(string exception)
        {
            this.WriteEvent(7, exception);
        }

        [Event(8, Level = EventLevel.Error, Version = 1)]
        public void CreateListener(string listenerName, ushort port, ushort readOnlyPort)
        {
            this.WriteEvent(8, listenerName, port, readOnlyPort);
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 1)]
        public void ConfigurationSettings(string environmentName, string tenant, string role, string ifxSessionName, string mdmAccountName)
        {
            this.WriteEvent(9, environmentName, tenant, role, ifxSessionName, mdmAccountName);
        }

        [Event(10, Level = EventLevel.LogAlways, Version = 1)]
        public void RingMaster_GetSetting(string settingName, string returnedValue)
        {
            this.WriteEvent(10, settingName, returnedValue);
        }

        [Event(11, Level = EventLevel.LogAlways, Version = 1)]
        public void RingMasterServiceTerminated(long elapsedSeconds)
        {
            this.WriteEvent(11, elapsedSeconds);
        }

        [Event(12, Level = EventLevel.LogAlways, Version = 1)]
        public void OnOpenAsync()
        {
            this.WriteEvent(12);
        }

        [Event(13, Level = EventLevel.Error, Version = 1)]
        public void OnOpenAsyncFailed(string exception)
        {
            this.WriteEvent(13, exception);
        }

        [Event(14, Level = EventLevel.LogAlways, Version = 1)]
        public void RunAsync()
        {
            this.WriteEvent(14);
        }

        [Event(15, Level = EventLevel.LogAlways, Version = 1)]
        public void RunAsyncCompleted(long elapsedMilliseconds)
        {
            this.WriteEvent(15, elapsedMilliseconds);
        }

        [Event(16, Level = EventLevel.LogAlways, Version = 1)]
        public void OnCloseAsync()
        {
            this.WriteEvent(16);
        }

        [Event(17, Level = EventLevel.Error, Version = 1)]
        public void OnCloseAsyncFailed(string exception)
        {
            this.WriteEvent(17, exception);
        }

        [Event(18, Level = EventLevel.LogAlways, Version = 1)]
        public void ListenerOpenAsync(string uri)
        {
            this.WriteEvent(18, uri);
        }

        [Event(19, Level = EventLevel.LogAlways, Version = 1)]
        public void ListenerCloseAsync(string uri)
        {
            this.WriteEvent(19, uri);
        }

        [Event(20, Level = EventLevel.LogAlways, Version = 1)]
        public void ListenerAbort(string uri)
        {
            this.WriteEvent(20, uri);
        }

        [Event(21, Level = EventLevel.LogAlways, Version = 1)]
        public void ListenerInitSession(string uri, string clientIP, string clientDigest)
        {
            this.WriteEvent(21, uri, clientIP, clientDigest);
        }
    }
}