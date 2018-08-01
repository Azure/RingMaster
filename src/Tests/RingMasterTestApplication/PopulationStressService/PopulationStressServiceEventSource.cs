// <copyright file="PopulationStressServiceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.PopulationStressService
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-PopulationStressService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class PopulationStressServiceEventSource : EventSource
    {
        static PopulationStressServiceEventSource()
        {
        }

        private PopulationStressServiceEventSource()
        {
        }

        public static PopulationStressServiceEventSource Log { get; } = new PopulationStressServiceEventSource();

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
        public void CreateAndDeleteHierarchyCreateStarted(string testPath, int maxNodes, int createBatchLength)
        {
            this.WriteEvent(8, testPath, maxNodes, createBatchLength);
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 2)]
        public void CreateAndDeleteHierarchyDeleteStarted(string testPath, int maxNodes, int deleteBatchLength, bool useScheduledDelete)
        {
            this.WriteEvent(9, testPath, maxNodes, deleteBatchLength, useScheduledDelete);
        }

        [Event(10, Level = EventLevel.LogAlways, Version = 1)]
        public void CreateAndDeleteHierarchyTestCompleted()
        {
            this.WriteEvent(10);
        }

        [Event(11, Level = EventLevel.LogAlways, Version = 1)]
        public void CreateAndDeleteHierarchyTestFailed(string exception)
        {
            this.WriteEvent(11, exception);
        }
    }
}
