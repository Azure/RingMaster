// <copyright file="RingMasterWatchdogEventSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterWatchdog
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Fabric-RingMasterWatchdog")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class RingMasterWatchdogEventSource : EventSource
    {
        public static RingMasterWatchdogEventSource Log { get; } = new RingMasterWatchdogEventSource();

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

        [Event(5, Level = EventLevel.LogAlways, Version = 1)]
        public void ReportStatus(string version, long uptimeInSeconds)
        {
           this.WriteEvent(5, version, uptimeInSeconds);
        }

        [Event(6, Level = EventLevel.LogAlways, Version = 1)]
        public void RunAsync()
        {
            this.WriteEvent(6);
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void RunAsyncFailed(string exception)
        {
            this.WriteEvent(7, exception);
        }

        [Event(8, Level = EventLevel.LogAlways, Version = 1)]
        public void RunAsyncCompleted(long uptimeInSeconds)
        {
            this.WriteEvent(8, uptimeInSeconds);
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 1)]
        public void ConfigurationSettings(string environmentName, string tenant, string role, string ifxSessionName, string mdmAccountName)
        {
            this.WriteEvent(9, environmentName, tenant, role, ifxSessionName, mdmAccountName);
        }

        [Event(10, Level = EventLevel.Informational, Version = 1)]
        public void Create(long iteration, string nodePath)
        {
            this.WriteEvent(10, iteration, nodePath);
        }

        [Event(11, Level = EventLevel.Informational, Version = 1)]
        public void Exists(long iteration, string nodePath)
        {
            this.WriteEvent(11, iteration, nodePath);
        }

        [Event(12, Level = EventLevel.Informational, Version = 1)]
        public void SetData(long iteration, string nodePath, int dataLength)
        {
            this.WriteEvent(12, iteration, nodePath, dataLength);
        }

        [Event(13, Level = EventLevel.Informational, Version = 1)]
        public void GetData(long iteration, string nodePath)
        {
            this.WriteEvent(13, iteration, nodePath);
        }

        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void GetDataFailed_RetrievedDataIsNull(long iteration, string nodePath, int expectedDataLength)
        {
            this.WriteEvent(14, iteration, nodePath, expectedDataLength);
        }

        [Event(15, Level = EventLevel.Informational, Version = 1)]
        public void GetDataFailed_RetrievedDataLengthMismatch(long iteration, string nodePath, int expectedDataLength, int retrievedDataLength)
        {
            this.WriteEvent(15, iteration, nodePath, expectedDataLength, retrievedDataLength);
        }

        [Event(16, Level = EventLevel.Informational, Version = 1)]
        public void GetDataFailed_RetrievedDataIsDifferent(long iteration, string nodePath, int expectedDataLength)
        {
            this.WriteEvent(16, iteration, nodePath, expectedDataLength);
        }

        [Event(17, Level = EventLevel.Informational, Version = 1)]
        public void Delete(long iteration, string nodePath, int version)
        {
            this.WriteEvent(17, iteration, nodePath, version);
        }

        [Event(18, Level = EventLevel.LogAlways, Version = 1)]
        public void TestRingMasterFunctionalitySucceeded(long iteration, long elapsedMilliseconds)
        {
            this.WriteEvent(18, iteration, elapsedMilliseconds);
        }

        [Event(19, Level = EventLevel.Error, Version = 1)]
        public void TestRingMasterFunctionalityFailed(long iteration, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(19, iteration, elapsedMilliseconds, exception);
        }

        [Event(20, Level = EventLevel.Error, Version = 1)]
        public void RunAsync_TransientException(long iteration, string exception)
        {
            this.WriteEvent(20, iteration, exception);
        }

        [Event(21, Level = EventLevel.LogAlways, Version = 1)]
        public void ConnectToRingMaster(long iteration, string connectionString)
        {
            this.WriteEvent(21, iteration, connectionString);
        }
    }
}
