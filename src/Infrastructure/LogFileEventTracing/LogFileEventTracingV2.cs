// <copyright file="LogFileEventTracingV2.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;

#nullable enable

    /// <summary>
    /// Listens ETW events and redirects them to log files in the specified, quota-controlled, directory.
    /// </summary>
    /// <remarks>
    /// If events or traces are produced more quickly than they can be written to files, the internal circular buffer
    /// will be full, and some oldest events will be dropped.
    ///
    /// This is a more efficient implementation compared to <see cref="LogFileEventTracing"/>. The old implementation would be removed in the next PR.
    /// </remarks>
    public sealed class LogFileEventTracingV2 : EventListener
    {
        /// <summary>
        /// When calculating the total size of log directory, only look at these files.
        /// </summary>
        private const string LogFileWildCard = "*.log";

        /// <summary>
        /// Bounded capacity of the trace queue before writing to disk
        /// </summary>
        /// <remarks>
        /// Note that potentially there will be this number of strings retained in the memory if the file writing is
        /// slow. Increasing this number will increase the total memory consumption proportionally.
        /// </remarks>
        private const int MaxTracesInBuffer = 1000 * 1000;

        /// <summary>
        /// Every time to delete old log files, aim at 90% capacity
        /// </summary>
        private const double LogDirectoryQuotaRatio = 0.9;

        /// <summary>
        /// Singleton instance of the <see cref="LogFileEventTracing"/> object
        /// </summary>
        private static LogFileEventTracingV2? instance;

        /// <summary>
        /// Cancellation source to indicate the thread should exit immediately
        /// </summary>
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        /// <summary>
        /// High-precision clock to record the elapsed time as replacement of wall-clock
        /// </summary>
        private readonly Stopwatch clock;

        /// <summary>
        /// Start time of the this tracing, for converting elapsed time to current date time
        /// </summary>
        private readonly DateTime startTime;

        /// <summary>
        /// Header of every log file
        /// </summary>
        private readonly string traceHeader;

        /// <summary>
        /// Function to return the log file name
        /// </summary>
        private readonly Func<string> createLogFileName;

        /// <summary>
        /// Directory where log files should be stored
        /// </summary>
        private readonly string logDirectory;

        /// <summary>
        /// Upper bound of total size of all log files in the given directory
        /// </summary>
        private readonly long logDirectoryQuotaInBytes;

        /// <summary>
        /// Upper bound of single log file size
        /// </summary>
        private readonly int logFileSize;

        /// <summary>
        /// Event source info of sources being listened
        /// </summary>
        private readonly Dictionary<string, EventSourceInfo> listenedEventSources;

        /// <summary>
        /// Queue of traces to be written to disk in the form of the circular buffer
        /// </summary>
        private readonly TraceRecord?[] traceBuffer = new TraceRecord[MaxTracesInBuffer];

        /// <summary>
        /// The current head in the buffer, or the number of the traces received, the next element is empty.
        /// </summary>
        private long receivedTraces = 0L;

        /// <summary>
        /// The current tail in the buffer, or the number of the traces written to files
        /// </summary>
        private long writtenTraces = -1L;

        /// <summary>
        /// Number of events being dropped because of slow file writing
        /// </summary>
        private long droppedTraces = 0L;

        /// <summary>
        /// Number of total written characters.
        /// </summary>
        private long totalWrittenChars = 0L;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogFileEventTracingV2"/> class
        /// </summary>
        /// <param name="logDirectory">Directory where log files should be stored</param>
        /// <param name="logDirectoryQuotaInBytes">Upper bound of total size of all log files in the given directory</param>
        /// <param name="logFileSize">Upper bound of single log file size</param>
        private LogFileEventTracingV2(string logDirectory, long logDirectoryQuotaInBytes, int logFileSize)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                throw new ArgumentNullException(nameof(logDirectory));
            }

            this.clock = Stopwatch.StartNew();
            this.startTime = DateTime.UtcNow;
            this.logDirectory = logDirectory;
            this.logDirectoryQuotaInBytes = logDirectoryQuotaInBytes;
            this.logFileSize = logFileSize;

            this.listenedEventSources = new Dictionary<string, EventSourceInfo>();

            var proc = Process.GetCurrentProcess();
            this.traceHeader = Assembly.GetCallingAssembly().GetCustomAttributes(false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()
                ?.InformationalVersion
                ?? "UnknownInformationalVersion";
            this.traceHeader = string.Format("### Process {0}, ID {1}, Machine {2}. {3}", proc.ProcessName, proc.Id, Environment.MachineName, this.traceHeader);

            this.traceBuffer[0] = TraceRecord.FromMessage(when: DateTime.UtcNow, $"### Trace started at {this.startTime}", traceNumber: 0);
            this.createLogFileName = () => string.Concat(proc.ProcessName, DateTime.UtcNow.ToString("--yyMMdd-HHmmss.fff--"), proc.Id, ".log");

            // Start the consumer thread which will never be stopped
            new Thread(this.WriteTraceToFile)
            {
                IsBackground = true,
            }.Start();
        }

        /// <summary>
        /// Gets the number traces received so far
        /// </summary>
        public static long ReceivedTraceCount => instance?.receivedTraces ?? 0L;

        /// <summary>
        /// Gets the number of traces dropped because of slow file writing
        /// </summary>
        public static long DroppedTraceCount => instance?.droppedTraces ?? 0L;

        /// <summary>
        /// Gets the total number of written characters.
        /// </summary>
        public static long TotalWrittenChars => instance?.totalWrittenChars ?? 0L;

        /// <summary>
        /// Gets the current time using <see cref="startTime"/> and <see cref="clock"/>.
        /// </summary>
        public DateTime TraceTime => this.startTime.Add(this.clock.Elapsed);

        /// <summary>
        /// Initializes the singleton of <see cref="LogFileEventTracing"/> class
        /// </summary>
        /// <param name="logDirectory">Directory where log files should be stored</param>
        /// <param name="logDirectoryQuotaInBytes">Upper bound of total size of all log files in the given directory</param>
        /// <param name="logFileSize">Upper bound of single log file size</param>
        public static void Start(
            string logDirectory,
            long logDirectoryQuotaInBytes = 1024L * 1024L * 1024L * 10L,
            int logFileSize = 1024 * 1024 * 10)
        {
            // Locking on the type is not always the most efficient way, but we call it once, so we're good here.
            lock (typeof(LogFileEventTracing))
            {
                instance ??= new LogFileEventTracingV2(logDirectory, logDirectoryQuotaInBytes, logFileSize);
            }
        }

        /// <summary>
        /// Stops the logging, writes remaining content to log file, and exits immediately
        /// </summary>
        public static void Stop()
        {
            if (instance != null && !instance.cancellation.IsCancellationRequested)
            {
                instance.cancellation.Cancel();
            }
        }

        /// <summary>
        /// Adds the given event source to listening
        /// </summary>
        /// <param name="eventSourceName">Full name of the event source to be listened</param>
        /// <param name="level">Event level</param>
        /// <param name="shortName">Short and friendly name of the event source</param>
        /// <param name="keywords">The keywords used for filtering some of the events.</param>
        /// <returns>True if the event source is listened successfully, false if otherwise</returns>
        public static bool AddEventSource(string eventSourceName, EventLevel level = EventLevel.Verbose, string? shortName = null, EventKeywords keywords = EventKeywords.None)
        {
            instance.ThrowIfNull();

            var eventSource = EventSource.GetSources().FirstOrDefault(s => s.Name == eventSourceName);
            if (eventSource != null)
            {
                instance.listenedEventSources.Add(eventSource.Name, EventSourceInfo.Parse(eventSource, shortName));
                instance.EnableEvents(eventSource, level, keywords);
                Trace($"Event source {eventSourceName} enabled with keywords {keywords}.");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds the specified line to the tracing
        /// </summary>
        /// <param name="line">Line to be added</param>
        public static void Trace(string line)
        {
            instance.ThrowIfNull();

            instance.Enqueue(line);
        }

        /// <inheritdoc />
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.Enqueue(eventData);
        }

        /// <summary>
        /// Deletes the oldest log files and returns the new size of all log files in the specified directory
        /// </summary>
        /// <param name="directory">Log directory</param>
        /// <param name="quota">Upper bound of all log files in byte</param>
        /// <returns>Actual total size of log files after the deletion</returns>
        private static long DeleteOldestLogFiles(string directory, long quota)
        {
            long totalSize = 0L;

            var dirInfo = new DirectoryInfo(directory);
            foreach (var fileInfo in dirInfo.EnumerateFiles(LogFileWildCard))
            {
                totalSize += fileInfo.Length;
            }

            if (totalSize > quota)
            {
                var logFiles = dirInfo.EnumerateFileSystemInfos(LogFileWildCard).OrderBy(x => x.CreationTimeUtc.Ticks);

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file.FullName);
                    totalSize -= fileInfo.Length;
                    fileInfo.Delete();

                    if (totalSize < quota * LogDirectoryQuotaRatio)
                    {
                        break;
                    }
                }
            }

            return totalSize;
        }

        private void Enqueue(EventWrittenEventArgs args)
        {
            var head = Interlocked.Increment(ref this.receivedTraces);
            var eventSourceName = args.EventSource.Name;
            var info = this.listenedEventSources[eventSourceName];
            var trace = TraceRecord.FromArgs(when: this.TraceTime, args, info, traceNumber: head);

            this.Enqueue(trace, head);
        }

        private void Enqueue(string message)
        {
            var head = Interlocked.Increment(ref this.receivedTraces);
            var trace = TraceRecord.FromMessage(when: this.TraceTime, message, traceNumber: head);

            this.Enqueue(trace, head);
        }

        private void Enqueue(TraceRecord trace, long head)
        {
            this.traceBuffer[unchecked((int)(head % MaxTracesInBuffer))] = trace;

            // Producer is too fast, let the consumer skip a trace.
            if (head - this.writtenTraces >= MaxTracesInBuffer - 1)
            {
                Interlocked.Increment(ref this.writtenTraces);
                Interlocked.Increment(ref this.droppedTraces);
            }
        }

#if NET6_0_OR_GREATER
        private StreamWriter OpenFile(string fullFileName)
        {
            // Open a stream writer with a bigger BufferSize that gives up to 20% better throughput compared to the
            // default constructor (which uses 8Kb buffer).
            StreamWriter sr = new StreamWriter(
                fullFileName,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                new FileStreamOptions()
                {
                    Access = FileAccess.Write,
                    BufferSize = 64 * 1024,
                    Mode = FileMode.Create,
                    Options = FileOptions.SequentialScan,
                    PreallocationSize = 0,
                    Share = FileShare.None,
                });

            return sr;
        }
#endif

        private void WriteTraceToFile()
        {
            var cancellationToken = this.cancellation;

            StreamWriter? file = null;

            int linefeedLength = 0;
            int fileSize = 0;

            if (!Directory.Exists(this.logDirectory))
            {
                Directory.CreateDirectory(this.logDirectory);
            }

            Directory.SetCurrentDirectory(this.logDirectory);

            long totalSize = DeleteOldestLogFiles(this.logDirectory, this.logDirectoryQuotaInBytes);
            var lineBuilder = new StringBuilder();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (file == null)
                    {
#if NET6_0_OR_GREATER
                        file = this.OpenFile(this.createLogFileName());
#else
                        file = File.CreateText(this.createLogFileName());
#endif
                        linefeedLength = file.NewLine.Length;
                        file.WriteLine(this.traceHeader);
                        fileSize = this.traceHeader.Length + linefeedLength;
                    }

                    try
                    {
                        if (this.writtenTraces < this.receivedTraces)
                        {
                            var nPos = unchecked((int)(Interlocked.Increment(ref this.writtenTraces) % MaxTracesInBuffer));
                            var trace = Interlocked.Exchange(ref this.traceBuffer[nPos], null);

                            // Race condition between producer and consumer when they write to the same location.
                            // Some traces are lost, however the producer is not blocked.
                            if (trace == null)
                            {
                                continue;
                            }

                            lineBuilder.Clear();
                            trace.ToString(lineBuilder);

#if NET6_0_OR_GREATER
                            // This version only exists in NET Core.
                            file.WriteLine(lineBuilder);
#else
                            file.WriteLine(lineBuilder.ToString());
#endif
                            fileSize += lineBuilder.Length + linefeedLength;
                            Interlocked.Add(ref this.totalWrittenChars, lineBuilder.Length + linefeedLength);
                        }
                        else
                        {
                            file.Flush();
                            Thread.Sleep(125);
                            continue;
                        }
                    }
                    catch (IOException)
                    {
                        // I/O error occurred during write. Close the file and start cleanup, hopefully the problem can be recovered.
                        fileSize = int.MaxValue;
                    }

                    if (fileSize > this.logFileSize)
                    {
                        file.Close();
                        file = null;

                        totalSize += fileSize;

                        if (totalSize > this.logDirectoryQuotaInBytes)
                        {
                            // Current file is closed already, no need to observe the cancellation
                            totalSize = DeleteOldestLogFiles(this.logDirectory, this.logDirectoryQuotaInBytes);
                        }
                    }
                }

                if (file is null)
                {
                    return;
                }

                // Process is terminating, write whatever left as soon as possible and get out of this thread.
                file.AutoFlush = true;
                file.WriteLine("### Logging flushing...");

                while (this.writtenTraces < this.receivedTraces)
                {
                    var nPos = unchecked((int)(Interlocked.Increment(ref this.writtenTraces) % MaxTracesInBuffer));
                    file.WriteLine(Interlocked.Exchange(ref this.traceBuffer[nPos], null));
                }

                file.WriteLine($"### Logging stopped. Dropped {this.droppedTraces}");

                file.Close();
                file = null;
            }
            finally
            {
                file?.Dispose();
            }
        }

        /// <summary>
        /// Contains information for a single trace.
        /// </summary>
        /// <remarks>
        /// Either <see cref="Message"/> is not null or <see cref="Args"/> and <see cref="Info"/> are not null.
        /// </remarks>
        private record TraceRecord(DateTime When, string? Message, EventWrittenEventArgs? Args, EventSourceInfo? Info, long TraceNumber)
        {
            public static TraceRecord FromArgs(DateTime when, EventWrittenEventArgs args, EventSourceInfo info, long traceNumber)
                => new TraceRecord(when, Message: null, args, info, traceNumber);

            public static TraceRecord FromMessage(DateTime when, string message, long traceNumber)
                => new TraceRecord(when, Message: message, Args: null, Info: null, traceNumber);

            public void ToString(StringBuilder sb)
            {
                if (this.Message is not null)
                {
                    sb.Append($"{this.TraceNumber} {this.When:O} {this.Message}");
                    return;
                }

                this.Info.ThrowIfNull();
                this.Args.ThrowIfNull();

                sb.Append($"{this.TraceNumber} {this.When:O} {this.Info.FriendlyName} [{this.Args.Level.ToStringFast()}] {this.Info.GetNameFromEventId(this.Args.EventId)} ");
                var parameters = this.Info.GetParametersFromEventId(this.Args.EventId);
                Debug.Assert(parameters?.Length == this.Args.Payload?.Count, "Even parameters array length should be the same as the payload collection");

                parameters.ThrowIfNull();

                if (this.Args.Payload != null)
                {
                    for (int i = 0; i < this.Args.Payload.Count; i++)
                    {
                        if (i != 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append($"{parameters[i]}={this.Args.Payload[i]}");
                    }
                }
            }
        }

        /// <summary>
        /// Metadata info of an event source
        /// </summary>
        private sealed class EventSourceInfo
        {
            /// <summary>
            /// Description of each event defined in the source
            /// </summary>
            private readonly Dictionary<int, (string Name, string?[] Parameters)> events = new Dictionary<int, (string Name, string?[] Parameters)>();

            private EventSourceInfo(string friendlyName)
            {
                this.FriendlyName = friendlyName;
            }

            /// <summary>
            /// Gets the friendly name of the event source
            /// </summary>
            public string FriendlyName { get; }

            /// <summary>
            /// Parses the <see cref="EventSource"/> class and returns the <see cref="EventSourceInfo"/> object
            /// </summary>
            /// <param name="eventSource">Event source to be parsed</param>
            /// <param name="friendlyName">Friendly name of the event source</param>
            /// <returns>event source info</returns>
            public static EventSourceInfo Parse(EventSource eventSource, string? friendlyName)
            {
                if (string.IsNullOrEmpty(friendlyName))
                {
                    friendlyName = eventSource.Name;
                }

                var info = new EventSourceInfo(friendlyName.ThrowIfNull());

                foreach (var method in eventSource.GetType().GetMethods())
                {
                    var eventAttribute = method.GetCustomAttribute<EventAttribute>();
                    if (eventAttribute == null)
                    {
                        continue;
                    }

                    var id = eventAttribute.EventId;
                    var name = method.Name;
                    var parameters = method.GetParameters().Select(p => p.Name).ToArray();

                    info.events.Add(id, (name, parameters));
                }

                return info;
            }

            /// <summary>
            /// Gets the name of the specified event ID
            /// </summary>
            /// <param name="id">Event ID</param>
            /// <returns>Name of the event</returns>
            public string GetNameFromEventId(int id)
            {
                return this.events[id].Name;
            }

            /// <summary>
            /// Gets the list of parameters of the specified event ID
            /// </summary>
            /// <param name="id">Event ID</param>
            /// <returns>List of parameters</returns>
            public string?[] GetParametersFromEventId(int id)
            {
                return this.events[id].Parameters;
            }
        }
    }
}
