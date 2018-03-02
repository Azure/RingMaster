// <copyright file="LogStreamEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.LogStream
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// LogStream trace event source.
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-LogStream")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class LogStreamEventSource : EventSource
    {
        private static readonly LogStreamEventSource LogInstance = new LogStreamEventSource();

        public LogStreamEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static LogStreamEventSource Log
        {
            get { return LogInstance; }
        }

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        public void NewFileCreated(string path)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("LogStream.NewFileCreated path={0}", path);
            }

            if (this.IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                this.WriteEvent(2, path);
            }
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void FileMarkedAsCompleted(string path)
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("LogStream.FileMarkedAsCompleted path={0}", path);
            }

            if (this.IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                this.WriteEvent(3, path);
            }
        }

        [Event(4, Level = EventLevel.Error, Version = 1)]
        public void MarkFileAsCompletedFailed(string path, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError("LogStream.MarkFileAsCompletedFailed path={0}, exception={1}", path, exception);
            }

            if (this.IsEnabled(EventLevel.Error, EventKeywords.None))
            {
                this.WriteEvent(4, path, exception);
            }
        }

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void WriterThreadStarted()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("LogStream.WriterThreadStarted");
            }

            if (this.IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                this.WriteEvent(5);
            }
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void WriterThreadTerminated()
        {
            if (this.TraceLevel >= TraceLevel.Info)
            {
                Trace.TraceInformation("LogStream.WriterThreadTerminated");
            }

            if (this.IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                this.WriteEvent(6);
            }
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void WriterThreadException(string currentFilePath, string exception)
        {
            if (this.TraceLevel >= TraceLevel.Error)
            {
                Trace.TraceError("LogStream.WriterThreadException currentFilePath={0}, exception={1}", currentFilePath, exception);
            }

            if (this.IsEnabled(EventLevel.Error, EventKeywords.None))
            {
                this.WriteEvent(7, currentFilePath, exception);
            }
        }
    }
}