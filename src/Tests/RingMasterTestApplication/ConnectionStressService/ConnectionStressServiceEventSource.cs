// <copyright file="ConnectionStressServiceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ConnectionStressService
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-ConnectionStressService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class ConnectionStressServiceEventSource : EventSource
    {
        static ConnectionStressServiceEventSource()
        {
        }

        private ConnectionStressServiceEventSource()
        {
        }

        public static ConnectionStressServiceEventSource Log { get; } = new ConnectionStressServiceEventSource();

        [Event(2, Level = EventLevel.LogAlways, Version = 1)]
        public void ConfigurationSettings(string environmentName, string tenant, string role, string ifxSessionName, string mdmAccountName)
        {
            this.WriteEvent(2, environmentName, tenant, role, ifxSessionName, mdmAccountName);
        }

        [Event(3, Level = EventLevel.LogAlways, Version = 1)]
        public void RegisterServiceSucceeded()
        {
            this.WriteEvent(3);
        }

        [Event(4, Level = EventLevel.LogAlways, Version = 1)]
        public void RegisterServiceFailed(string exception)
        {
            this.WriteEvent(4, exception);
        }

        [Event(5, Level = EventLevel.LogAlways, Version = 1)]
        public void ReportServiceStatus(string version, long uptimeInSeconds)
        {
            this.WriteEvent(5, version, uptimeInSeconds);
        }

        [Event(6, Level = EventLevel.Error, Version = 1)]
        public void RunAsyncFailed(string exception)
        {
            this.WriteEvent(6, exception);
        }

        [Event(7, Level = EventLevel.LogAlways, Version = 1)]
        public void Terminated(long uptimeInSeconds)
        {
            this.WriteEvent(7, uptimeInSeconds);
        }

        [Event(8, Level = EventLevel.LogAlways, Version = 1)]
        public void ConnectPerformanceTestStarted(string testPath, int numConnections, int minConnectionLifetimeInSeconds, int maxConnectionLifetimeInSeconds)
        {
            this.WriteEvent(8, testPath, numConnections, minConnectionLifetimeInSeconds, maxConnectionLifetimeInSeconds);
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 1)]
        public void ConnectPerformanceTestCompleted()
        {
            this.WriteEvent(9);
        }

        [Event(10, Level = EventLevel.Error, Version = 1)]
        public void ConnectPerformanceTestFailed(string exception)
        {
            this.WriteEvent(10, exception);
        }

        [Event(11, Level = EventLevel.Informational, Version = 1)]
        public void CreateConnection(bool secureConnection, uint protocolVersion, long maxConnectionLifespan)
        {
            this.WriteEvent(11, secureConnection, protocolVersion, maxConnectionLifespan);
        }

        [Event(12, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionCreated(int connectionCount, long elapsedMs)
        {
            this.WriteEvent(12, connectionCount, elapsedMs);
        }

        [Event(13, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionEstablished(string remoteEndPoint, string remoteIdentity, long setupTimeMs)
        {
            this.WriteEvent(13, remoteEndPoint, remoteIdentity, setupTimeMs);
        }

        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionClosed(long connectionId, string remoteEndPoint, string remoteIdentity)
        {
            this.WriteEvent(14, connectionId, remoteEndPoint, remoteIdentity);
        }

        [Event(15, Level = EventLevel.Error, Version = 1)]
        public void EstablishConnectionFailed(long processingTimeMs)
        {
            this.WriteEvent(15, processingTimeMs);
        }
    }
}
