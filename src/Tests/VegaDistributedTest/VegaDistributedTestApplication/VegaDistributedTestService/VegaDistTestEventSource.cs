// <copyright file="VegaDistTestEventSource.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// The vega distributed test event source.
    /// </summary>
    /// <seealso cref="System.Diagnostics.Tracing.EventSource" />
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-DistributedTestService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class VegaDistTestEventSource : EventSource
    {
        static VegaDistTestEventSource()
        {
        }

        private VegaDistTestEventSource()
        {
        }

        /// <summary>
        /// Gets the log.
        /// </summary>
        /// <value>
        /// The log.
        /// </value>
        public static VegaDistTestEventSource Log { get; } = new VegaDistTestEventSource();

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
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

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void RegisterServiceBegin()
        {
            this.WriteEvent(5);
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void RegisterListenersSucceeded()
        {
            this.WriteEvent(6);
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void RegisterListenersFailed(string exception)
        {
            this.WriteEvent(7, exception);
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void RunAsyncCanceled()
        {
            this.WriteEvent(8);
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void RunAsyncCompleted()
        {
            this.WriteEvent(9);
        }

        [Event(10, Level = EventLevel.Error, Version = 1)]
        public void SetMdmDimensionFailed(uint errorCode, string errorMessage)
        {
            this.WriteEvent(10, errorCode, errorMessage);
        }

        [Event(11, Level = EventLevel.Error, Version = 1)]
        public void ParseEndpointsStringFailed(string endpoint)
        {
            this.WriteEvent(11, endpoint);
        }

        [Event(12, Level = EventLevel.Error, Version = 1)]
        public void GetEndpointAddressFailed(string listenerName, string replicaAddress)
        {
            this.WriteEvent(12, listenerName, replicaAddress);
        }

        [Event(13, Level = EventLevel.Error, Version = 1)]
        public void RunJobOnClientFailed(string jobControlEndpoint, string exception)
        {
            this.WriteEvent(13, jobControlEndpoint, exception);
        }

        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void JobCancelRequested()
        {
            this.WriteEvent(14);
        }

        [Event(15, Level = EventLevel.Informational, Version = 1)]
        public void StartJob(string scenario, string paramString)
        {
            this.WriteEvent(15, scenario, paramString);
        }

        [Event(16, Level = EventLevel.Informational, Version = 1)]
        public void StartJobCancelled(string scenario)
        {
            this.WriteEvent(16, scenario);
        }

        [Event(17, Level = EventLevel.Error, Version = 1)]
        public void StartJobFailed(string scenario, string exception)
        {
            this.WriteEvent(17, scenario, exception);
        }

        [Event(18, Level = EventLevel.Error, Version = 1)]
        public void ScheduleJobFailed(string scenario, string exception)
        {
            this.WriteEvent(18, scenario, exception);
        }

        [Event(19, Level = EventLevel.Informational, Version = 1)]
        public void JobScheduled(string scenario)
        {
            this.WriteEvent(19, scenario);
        }

        [Event(20, Level = EventLevel.Informational, Version = 1)]
        public void JobCompleted(string job)
        {
            this.WriteEvent(20, job);
        }

        [Event(21, Level = EventLevel.Informational, Version = 1)]
        public void WcfRequestProcessing(
            string contract,
            string action,
            Guid messageId,
            string fromAddress,
            bool isFault,
            long durationInMs,
            string additional)
        {
            this.WriteEvent(21, contract, action, messageId, fromAddress, isFault, durationInMs, additional);
        }

        [Event(22, Level = EventLevel.Informational, Version = 1)]
        public void GeneralMessage(string message)
        {
            this.WriteEvent(22, message);
        }
    }
}