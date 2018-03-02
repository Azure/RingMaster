// <copyright file="PopulationStressServiceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.PopulationStressService
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-PopulationStressService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class PopulationStressServiceEventSource : EventSource
    {
        public PopulationStressServiceEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static PopulationStressServiceEventSource Log { get; } = new PopulationStressServiceEventSource();

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.LogAlways, Version = 1)]
        public void ConfigurationSettings(string environmentName, string tenant, string role, string ifxSessionName, string mdmAccountName)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"PopulationStressService.ConfigurationSettings environmentName={environmentName}, tenant={tenant}, role={role}, ifxSessionName={ifxSessionName}, mdmAccountName={mdmAccountName}");
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
                Trace.TraceInformation($"PopulationStressService.RegisterServiceSucceeded");
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
                Trace.TraceError($"PopulationStressService.RegisterServiceFailed exception={exception}");
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
                Trace.TraceInformation($"PopulationStressService.Status version={version}, uptimeInSeconds={uptimeInSeconds}");
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
                Trace.TraceError($"PopulationStressService.RunAsync-Failed exception={exception}");
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
                Trace.TraceInformation($"PopulationStressService.Terminated uptimeInSeconds={uptimeInSeconds}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(7, uptimeInSeconds);
            }
        }

        [Event(8, Level = EventLevel.LogAlways, Version = 1)]
        public void CreateAndDeleteHierarchyCreateStarted(string testPath, int maxNodes, int createBatchLength)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"PopulationStressService.CreateAndDeleteHierarchyTest-CreateStarted testPath={testPath}, maxNodes={maxNodes}, createBatchLength={createBatchLength}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(8, testPath, maxNodes, createBatchLength);
            }
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 2)]
        public void CreateAndDeleteHierarchyDeleteStarted(string testPath, int maxNodes, int deleteBatchLength, bool useScheduledDelete)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"PopulationStressService.CreateAndDeleteHierarchyTest-DeleteStarted testPath={testPath}, maxNodes={maxNodes}, deleteBatchLength={deleteBatchLength}, useScheduledDelete={useScheduledDelete}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(9, testPath, maxNodes, deleteBatchLength, useScheduledDelete);
            }
        }

        [Event(10, Level = EventLevel.LogAlways, Version = 1)]
        public void CreateAndDeleteHierarchyTestCompleted()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"PopulationStressService.CreateAndDeleteHierarchyTest-Completed");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(10);
            }
        }

        [Event(11, Level = EventLevel.LogAlways, Version = 1)]
        public void CreateAndDeleteHierarchyTestFailed(string exception)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation($"PopulationStressService.CreateAndDeleteHierarchyTest-Failed exception={exception}");
            }

            if (this.IsEnabled())
            {
                this.WriteEvent(11, exception);
            }
        }
    }
}