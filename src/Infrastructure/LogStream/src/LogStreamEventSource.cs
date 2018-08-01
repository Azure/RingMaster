// <copyright file="LogStreamEventSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
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
        static LogStreamEventSource()
        {
        }

        private LogStreamEventSource()
        {
        }

        public static LogStreamEventSource Log { get; } = new LogStreamEventSource();

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        public void NewFileCreated(string path)
        {
            this.WriteEvent(2, path);
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void FileMarkedAsCompleted(string path)
        {
            this.WriteEvent(3, path);
        }

        [Event(4, Level = EventLevel.Error, Version = 1)]
        public void MarkFileAsCompletedFailed(string path, string exception)
        {
            this.WriteEvent(4, path, exception);
        }

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void WriterThreadStarted()
        {
            this.WriteEvent(5);
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void WriterThreadTerminated()
        {
            this.WriteEvent(6);
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void WriterThreadException(string currentFilePath, string exception)
        {
            this.WriteEvent(7, currentFilePath, exception);
        }
    }
}
