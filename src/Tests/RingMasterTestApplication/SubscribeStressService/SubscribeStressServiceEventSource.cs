// <copyright file="SubscribeStressServiceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.SubscribeStressService
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-SubscribeStressService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class SubscribeStressServiceEventSource : EventSource
    {
        public SubscribeStressServiceEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static SubscribeStressServiceEventSource Log { get; } = new SubscribeStressServiceEventSource();

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.LogAlways, Version = 1)]
        public void ConfigurationSettings(string environmentName, string tenant, string role, string ifxSessionName, string mdmAccountName)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SubscribeStressService.ConfigurationSettings environmentName={environmentName}, tenant={tenant}, role={role}, ifxSessionName={ifxSessionName}, mdmAccountName={mdmAccountName}");
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
                Trace.TraceInformation($"SubscribeStressService.RegisterServiceSucceeded");
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
                Trace.TraceError($"SubscribeStressService.RegisterServiceFailed exception={exception}");
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
                Trace.TraceInformation($"SubscribeStressService.Status version={version}, uptimeInSeconds={uptimeInSeconds}");
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
                Trace.TraceError($"SubscribeStressService.RunAsync-Failed exception={exception}");
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
                Trace.TraceInformation($"SubscribeStressService.Terminated uptimeInSeconds={uptimeInSeconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(7, uptimeInSeconds);
            }
        }

        [Event(8, Level = EventLevel.LogAlways, Version = 1)]
        public void WatcherPerformanceTestStarted(string testPath, int maxNodes, int maxConcurrentWatchers)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"SubscribeStressService.WatcherPerformanceTest-Started testPath={testPath}, maxNodes={maxNodes}, maxConcurrentWatchers={maxConcurrentWatchers}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(8, testPath, maxNodes, maxConcurrentWatchers);
            }
        }

        [Event(9, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherSet()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SubscribeStressService.WatcherSet");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(9);
            }
        }

        [Event(10, Level = EventLevel.Error, Version = 1)]
        public void WatcherSetFailed()
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError($"SubscribeStressService.WatcherSet-Failed");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(10);
            }
        }

        [Event(11, Level = EventLevel.Verbose, Version = 1)]
        public void WatcherNotified()
        {
            if (this.TraceLevel >= TraceLevel.Verbose)
            {
                Trace.TraceInformation($"SubscribeStressService.WatcherNotified");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11);
            }
        }
    }
}