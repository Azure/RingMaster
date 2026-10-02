// <copyright file="VegaDistTestEventSource.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Diagnostics.Tracing;
    using Microsoft.Vega.Test.Helpers;

    /// <summary>
    /// The vega distributed test event source.
    /// </summary>
    /// <seealso cref="System.Diagnostics.Tracing.EventSource" />
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-DistributedTestService")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    public sealed class VegaDistTestEventSource : EventSource
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

        /// <summary>
        /// Unhandleds the exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="isTerminating">if set to <c>true</c> [is terminating].</param>
        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        [Event(2, Level = EventLevel.LogAlways, Version = 1)]
        public void UnhandledException(string exception, bool isTerminating)
        {
            this.WriteEvent(2, exception, isTerminating);
        }

        /// <summary>
        /// Registers the service succeeded.
        /// </summary>
        [Event(3, Level = EventLevel.LogAlways, Version = 1)]
        public void RegisterServiceSucceeded()
        {
            this.WriteEvent(3);
        }

        /// <summary>
        /// Registers the service failed.
        /// </summary>
        /// <param name="exception">The exception.</param>
        [Event(4, Level = EventLevel.Error, Version = 1)]
        public void RegisterServiceFailed(string exception)
        {
            this.WriteEvent(4, exception);
        }

        /// <summary>
        /// Registers the service begin.
        /// </summary>
        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void RegisterServiceBegin()
        {
            this.WriteEvent(5);
        }

        /// <summary>
        /// Registers the listeners succeeded.
        /// </summary>
        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void RegisterListenersSucceeded()
        {
            this.WriteEvent(6);
        }

        /// <summary>
        /// Registers the listeners failed.
        /// </summary>
        /// <param name="exception">The exception.</param>
        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void RegisterListenersFailed(string exception)
        {
            this.WriteEvent(7, exception);
        }

        /// <summary>
        /// Runs the asynchronous canceled.
        /// </summary>
        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void RunAsyncCanceled()
        {
            this.WriteEvent(8);
        }

        /// <summary>
        /// Runs the asynchronous completed.
        /// </summary>
        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void RunAsyncCompleted()
        {
            this.WriteEvent(9);
        }

        /// <summary>
        /// Sets the MDM dimension failed.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        [Event(10, Level = EventLevel.Error, Version = 1)]
        public void SetMdmDimensionFailed(uint errorCode, string errorMessage)
        {
            this.WriteEvent(10, errorCode, errorMessage);
        }

        /// <summary>
        /// Parses the endpoints string failed.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        [Event(11, Level = EventLevel.Error, Version = 1)]
        public void ParseEndpointsStringFailed(string endpoint)
        {
            this.WriteEvent(11, endpoint);
        }

        /// <summary>
        /// Gets the endpoint address failed.
        /// </summary>
        /// <param name="listenerName">Name of the listener.</param>
        /// <param name="replicaAddress">The replica address.</param>
        [Event(12, Level = EventLevel.Error, Version = 1)]
        public void GetEndpointAddressFailed(string listenerName, string replicaAddress)
        {
            this.WriteEvent(12, listenerName, replicaAddress);
        }

        /// <summary>
        /// Runs the job on client failed.
        /// </summary>
        /// <param name="exception">The exception.</param>
        [Event(13, Level = EventLevel.Error, Version = 2)]
        public void RunJobOnClientFailed(string exception)
        {
            this.WriteEvent(13, exception);
        }

        /// <summary>
        /// Jobs the cancel requested.
        /// </summary>
        [Event(14, Level = EventLevel.Informational, Version = 1)]
        public void JobCancelRequested()
        {
            this.WriteEvent(14);
        }

        /// <summary>
        /// Starts the job.
        /// </summary>
        [Event(15, Level = EventLevel.Informational, Version = 2)]
        public void StartJob()
        {
            this.WriteEvent(15);
        }

        /// <summary>
        /// Starts the job cancelled.
        /// </summary>
        [Event(16, Level = EventLevel.Informational, Version = 2)]
        public void StartJobCancelled()
        {
            this.WriteEvent(16);
        }

        /// <summary>
        /// Starts the job failed.
        /// </summary>
        /// <param name="exception">The exception.</param>
        [Event(17, Level = EventLevel.Error, Version = 2)]
        public void StartJobFailed(string exception)
        {
            this.WriteEvent(17, exception);
        }

        /// <summary>
        /// Schedules the job failed.
        /// </summary>
        /// <param name="exception">The exception.</param>
        [Event(18, Level = EventLevel.Error, Version = 2)]
        public void ScheduleJobFailed(string exception)
        {
            this.WriteEvent(18, exception);
        }

        /// <summary>
        /// Jobs the scheduled.
        /// </summary>
        [Event(19, Level = EventLevel.Informational, Version = 2)]
        public void JobScheduled()
        {
            this.WriteEvent(19);
        }

        /// <summary>
        /// Jobs the completed.
        /// </summary>
        /// <param name="job">The job.</param>
        [Event(20, Level = EventLevel.Informational, Version = 1)]
        public void JobCompleted(string job)
        {
            this.WriteEvent(20, job);
        }

        /// <summary>
        /// WCFs the request processing.
        /// </summary>
        /// <param name="contract">The contract.</param>
        /// <param name="action">The action.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="fromAddress">From address.</param>
        /// <param name="isFault">if set to <c>true</c> [is fault].</param>
        /// <param name="durationInMs">The duration in ms.</param>
        /// <param name="additional">The additional.</param>
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

        /// <summary>
        /// Generals the message.
        /// </summary>
        /// <param name="message">The message.</param>
        [Event(22, Level = EventLevel.Informational, Version = 1)]
        public void GeneralMessage(string message)
        {
            this.WriteEvent(22, message);
        }

        /// <summary>
        /// Operations the throughput.
        /// </summary>
        /// <param name="operationType">Type of the operation.</param>
        /// <param name="throughput">The throughput.</param>
        [Event(23, Level = EventLevel.LogAlways, Version = 1)]
        public void OperationThroughput(OperationType operationType, double throughput)
        {
            this.WriteEvent(23, operationType, throughput);
        }

        /// <summary>
        /// Operations the latency.
        /// </summary>
        /// <param name="operationType">Type of the operation.</param>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="average">The average.</param>
        /// <param name="p90">The P90.</param>
        /// <param name="p99">The P99.</param>
        [Event(24, Level = EventLevel.LogAlways, Version = 1)]
        public void OperationLatency(OperationType operationType, double minimum, double maximum, double average, double p90, double p99)
        {
            this.WriteEvent(24, operationType, minimum, maximum, average, p90, p99);
        }

        /// <summary>
        /// Starts the job.
        /// </summary>
        /// <param name="scenario">The scenario.</param>
        /// <param name="paramString">The parameter string.</param>
        [Event(25, Level = EventLevel.Informational, Version = 1)]
        public void InitializeJob(string scenario, string paramString)
        {
            this.WriteEvent(25, scenario, paramString);
        }
    }
}