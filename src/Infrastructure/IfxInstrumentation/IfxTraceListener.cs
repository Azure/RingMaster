// <copyright file="IfxTraceListener.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    /// A <see cref="TraceListener"/> that writes trace events to <c>Ifx</c>.
    /// </summary>
    internal sealed class IfxTraceListener : TraceListener
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IfxTraceListener"/> class.
        /// </summary>
        public IfxTraceListener()
        {
        }

        /// <summary>
        /// Writes trace information, a formatted array of objects and event information to <c>Ifx</c>.
        /// </summary>
        /// <param name="eventCache">A <see cref="TraceEventCache"/> object that contains the current process ID, thread ID and stack trace information</param>
        /// <param name="source">Name used to identify the source of the event</param>
        /// <param name="eventType">One of the <see cref="TraceEventType"/> values specifying the type of event that has caused the trace</param>
        /// <param name="id">A numeric identifier for the event</param>
        /// <param name="format">A format string that contains zero or more format items, which correspond to objects in the args array</param>
        /// <param name="args">An object array containing zero or more objects to format</param>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            var tracingLevel = GetIfxTracingLevel(eventType);

            IfxTracer.LogMessage(
                tracingLevel,
                source,
                string.Format(format, args));
        }

        /// <summary>
        /// Writes trace information, a message and event information to <c>Ifx</c>.
        /// </summary>
        /// <param name="eventCache">A <see cref="TraceEventCache"/> object that contains the current process ID, thread ID and stack trace information</param>
        /// <param name="source">Name used to identify the source of the event</param>
        /// <param name="eventType">One of the <see cref="TraceEventType"/> values specifying the type of event that has caused the trace</param>
        /// <param name="id">A numeric identifier for the event</param>
        /// <param name="message">A message to write</param>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            this.TraceEvent(eventCache, source, eventType, id, "{0}", message);
        }

        /// <summary>
        /// Writes the specified message to <c>Ifx</c>.
        /// </summary>
        /// <param name="message">A message to write</param>
        public override void Write(string message)
        {
            this.TraceEvent(null, null, TraceEventType.Verbose, 99, "{0}", message);
        }

        /// <summary>
        /// Writes the specified message to <c>Ifx</c>
        /// </summary>
        /// <param name="message">A message to write</param>
        public override void WriteLine(string message)
        {
            this.TraceEvent(null, null, TraceEventType.Verbose, 99, "{0}", message);
        }

        /// <summary>
        /// Gets the <see cref="IfxTracingLevel"/> that corresponds to the given <see cref="TraceEventType"/>.
        /// </summary>
        /// <param name="eventType">Type of the Trace event</param>
        /// <returns>A <see cref="IfxTracingLevel"/> value that corresponds to the given <see cref="TraceEventType"/></returns>
        private static IfxTracingLevel GetIfxTracingLevel(TraceEventType eventType)
        {
            // Map TraceEventType.Information to IfxTracingLevel.Verbose to avoid overwhelming MDS IFX upload.
            switch (eventType)
            {
                case TraceEventType.Critical: return IfxTracingLevel.Critical;
                case TraceEventType.Error: return IfxTracingLevel.Error;
                case TraceEventType.Warning: return IfxTracingLevel.Warning;
                case TraceEventType.Information: return IfxTracingLevel.Informational;
                default: return IfxTracingLevel.Verbose;
            }
        }

        /// <summary>
        /// Gets a string that describes the given <see cref="IfxTracingLevel"/>.
        /// </summary>
        /// <param name="level">The given <see cref="IfxTracingLevel"/> value</param>
        /// <returns>A string that describes the given value</returns>
        private static string GetTracingLevelName(IfxTracingLevel level)
        {
            switch (level)
            {
                case IfxTracingLevel.Critical: return "critical";
                case IfxTracingLevel.Error: return "error";
                case IfxTracingLevel.Warning: return "warning";
                case IfxTracingLevel.Informational: return "information";
                default: return "verbose";
            }
        }
    }
}