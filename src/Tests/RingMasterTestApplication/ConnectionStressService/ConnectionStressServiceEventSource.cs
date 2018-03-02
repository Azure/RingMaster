// <copyright file="ConnectionStressServiceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ConnectionStressService
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-ConnectionStressService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class ConnectionStressServiceEventSource : EventSource
    {
        public ConnectionStressServiceEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static ConnectionStressServiceEventSource Log { get; } = new ConnectionStressServiceEventSource();

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.LogAlways, Version = 1)]
        public void ConfigurationSettings(string environmentName, string tenant, string role, string ifxSessionName, string mdmAccountName)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ConnectionStressService.ConfigurationSettings environmentName={environmentName}, tenant={tenant}, role={role}, ifxSessionName={ifxSessionName}, mdmAccountName={mdmAccountName}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(2, environmentName, tenant, role, ifxSessionName, mdmAccountName);
            }
        }

        [Event(3, Level = EventLevel.LogAlways, Version = 1)]
        public void RegisterServiceSucceeded()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ConnectionStressService.RegisterServiceSucceeded");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(3);
            }
        }

        [Event(4, Level = EventLevel.LogAlways, Version = 1)]
        public void RegisterServiceFailed(string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"ConnectionStressService.RegisterServiceFailed exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(4, exception);
            }
        }

        [Event(5, Level = EventLevel.LogAlways, Version = 1)]
        public void ReportServiceStatus(string version, long uptimeInSeconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ConnectionStressService.Status version={version}, uptimeInSeconds={uptimeInSeconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(5, version, uptimeInSeconds);
            }
        }

        [Event(6, Level = EventLevel.Error, Version = 1)]
        public void RunAsyncFailed(string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"ConnectionStressService.RunAsync-Failed exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(6, exception);
            }
        }

        [Event(7, Level = EventLevel.LogAlways, Version = 1)]
        public void Terminated(long uptimeInSeconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ConnectionStressService.Terminated uptimeInSeconds={uptimeInSeconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(7, uptimeInSeconds);
            }
        }

        [Event(8, Level = EventLevel.LogAlways, Version = 1)]
        public void ConnectPerformanceTestStarted(string testPath, int numConnections, int minConnectionLifetimeInSeconds, int maxConnectionLifetimeInSeconds)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation(
                    $"ConnectionStressService.ConnectPerformanceTest-Started testPath={testPath}, numConnections={numConnections}, "
                    + $"minConnectionLifetimeInSeconds={minConnectionLifetimeInSeconds}, maxConnectionLifetimeInSeconds={maxConnectionLifetimeInSeconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(8, testPath, numConnections, minConnectionLifetimeInSeconds, maxConnectionLifetimeInSeconds);
            }
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 1)]
        public void ConnectPerformanceTestCompleted()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ConnectionStressService.ConnectPerformanceTest-Completed");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(9);
            }
        }

        [Event(10, Level = EventLevel.Error, Version = 1)]
        public void ConnectPerformanceTestFailed(string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"ConnectionStressService.ConnectPerformanceTest-Failed exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(10, exception);
            }
        }

        [Event(11, Level = EventLevel.Informational, Version = 1)]
        public void CreateConnection(bool secureConnection, uint protocolVersion, long maxConnectionLifespan)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ConnectionStressService.CreateConnection secureConnection={secureConnection}, protocolVersion={protocolVersion}, maxConnectionLifespan={maxConnectionLifespan}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11, secureConnection, protocolVersion, maxConnectionLifespan);
            }
        }

        [Event(12, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionCreated(int connectionCount, long elapsedMs)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"ConnectionStressService.ConnectionCreated connectionCount={connectionCount}, elapsedMs={elapsedMs}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(12, connectionCount, elapsedMs);
            }
        }

        [Event(13, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionEstablished(string remoteEndPoint, string remoteIdentity, long setupTimeMs)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ConnectionStressService.ConnectionEstablished remoteEndPoint={remoteEndPoint}, remoteIdentity={remoteIdentity}, setupTimeMs={setupTimeMs}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(13, remoteEndPoint, remoteIdentity, setupTimeMs);
            }
        }

        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void ConnectionClosed(long connectionId, string remoteEndPoint, string remoteIdentity)
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"ConnectionStressService.ConnectionClosed connectionId={connectionId}, remoteEndPoint={remoteEndPoint}, remoteIdentity={remoteIdentity}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(14, connectionId, remoteEndPoint, remoteIdentity);
            }
        }

        [Event(15, Level = EventLevel.Error, Version = 1)]
        public void EstablishConnectionFailed(long processingTimeMs)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceInformation($"ConnectionStressService.EstablishConnectionFailed processingTimeMs={processingTimeMs}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(15, processingTimeMs);
            }
        }
    }
}