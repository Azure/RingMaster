// <copyright file="LogFileEventTracing.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// Listens ETW events and redirects them to log files in the specified, quota-controlled, directory.
    /// </summary>
    /// <remarks>
    /// Performance under multi-thread environment (hundreds or thousands calling Trace concurrently) is yet to be tuned.
    /// </remarks>
    public sealed class LogFileEventTracing : EventListener
    {
        /// <summary>
        /// When calculating the total size of log directory, only look at these files.
        /// </summary>
        private const string LogFileWildCard = "*.log";

        /// <summary>
        /// Bounded capacity of the trace queue before writing to disk
        /// </summary>
        private const int MaxTracesInQueue = 1000 * 1000;

        /// <summary>
        /// Interval to flush the current log file
        /// </summary>
        private const int LogFileFlushInterval = 1000 * 10;

        /// <summary>
        /// Every time to delete old log files, aim at 90% capacity
        /// </summary>
        private const double LogDirectoryQuotaRatio = 0.9;

        /// <summary>
        /// Singleton instance of the <see cref="LogFileEventTracing"/> object
        /// </summary>
        private static LogFileEventTracing instance;

        /// <summary>
        /// Cancellation source to indicate the thread should exit immediately
        /// </summary>
        private readonly CancellationTokenSource cancellation;

        /// <summary>
        /// High-precision clock to record the elapsed time as replacement of wall-clock
        /// </summary>
        private readonly Stopwatch clock;

        /// <summary>
        /// Start time of the this tracing, for converting elapsed time to current date time
        /// </summary>
        private readonly DateTime startTime;

        /// <summary>
        /// Queue of traces to be written to disk
        /// </summary>
        private readonly BlockingCollection<string> traceQueue;

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
        /// Initializes a new instance of the <see cref="LogFileEventTracing" /> class
        /// </summary>
        /// <param name="logDirectory">Directory where log files should be stored</param>
        /// <param name="logDirectoryQuotaInBytes">Upper bound of total size of all log files in the given directory</param>
        /// <param name="logFileSize">Upper bound of single log file size</param>
        private LogFileEventTracing(string logDirectory, long logDirectoryQuotaInBytes, int logFileSize)
        {
            if (string.IsNullOrEmpty(logDirectory))
            {
                throw new ArgumentNullException(nameof(logDirectory));
            }

            this.cancellation = new CancellationTokenSource();
            this.clock = Stopwatch.StartNew();
            this.startTime = DateTime.UtcNow;
            this.logDirectory = logDirectory;
            this.logDirectoryQuotaInBytes = logDirectoryQuotaInBytes;
            this.logFileSize = logFileSize;

            this.listenedEventSources = new Dictionary<string, EventSourceInfo>();

            this.traceQueue = new BlockingCollection<string>(MaxTracesInQueue);

            var proc = Process.GetCurrentProcess();
            this.traceHeader = Assembly.GetCallingAssembly().GetCustomAttributes(false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()
                ?.InformationalVersion
                ?? "UnknownInformationalVersion";
            this.traceHeader = string.Format("### Process {0}, ID {1}, Machine {2}. {3}", proc.ProcessName, proc.Id, Environment.MachineName, this.traceHeader);

            this.createLogFileName = () => string.Concat(proc.ProcessName, DateTime.UtcNow.ToString("--yyMMdd-HHmmss.fff--"), proc.Id, ".log");

            // Start the consumer thread which will never be stopped
            new Thread(this.WriteTraceToFile)
            {
                Priority = ThreadPriority.Highest,
            }
            .Start(this.cancellation.Token);
        }

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
            lock (typeof(LogFileEventTracing))
            {
                if (instance != null)
                {
                    return;
                }
                else
                {
                    instance = new LogFileEventTracing(logDirectory, logDirectoryQuotaInBytes, logFileSize);
                }
            }
        }

        /// <summary>
        /// Stops the logging, writes remaining content to log file, and exits immediately
        /// </summary>
        public static void Stop()
        {
            if (instance != null && !instance.cancellation.IsCancellationRequested)
            {
                instance.traceQueue.CompleteAdding();
                instance.cancellation.Cancel();
            }
        }

        /// <summary>
        /// Adds the given event source to listening
        /// </summary>
        /// <param name="eventSourceName">Full name of the event source to be listened</param>
        /// <param name="level">Event level</param>
        /// <param name="shortName">Short and friendly name of the event source</param>
        /// <returns>If the event source is listened</returns>
        public static bool AddEventSource(string eventSourceName, EventLevel level = EventLevel.Verbose, string shortName = null)
        {
            if (instance == null)
            {
                throw new InvalidOperationException();
            }

            var eventSource = EventSource.GetSources().FirstOrDefault(s => s.Name == eventSourceName);
            if (eventSource != null)
            {
                instance.listenedEventSources.Add(eventSource.Name, EventSourceInfo.Parse(eventSource, shortName));
                instance.EnableEvents(eventSource, level);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the specified line to the tracing
        /// </summary>
        /// <param name="line">Line to be added</param>
        public static void Trace(string line)
        {
            if (instance == null)
            {
                throw new InvalidOperationException();
            }

            var timestamp = instance.startTime.Add(instance.clock.Elapsed).ToString("O");

            try
            {
                instance.traceQueue.Add(string.Join(" ", timestamp, line));
            }
            catch (InvalidOperationException)
            {
                // The queue is completed adding, no more logs are accepted.
            }
        }

        /// <summary>
        /// Called when an event is written
        /// </summary>
        /// <param name="eventData">Event data</param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData == null)
            {
                return;
            }

            var timestamp = this.startTime.Add(this.clock.Elapsed).ToString("O");
            var eventSourceName = eventData.EventSource.Name;
            var eventId = eventData.EventId;
            var info = this.listenedEventSources[eventSourceName];

            var message = eventData.Message;
            if (string.IsNullOrEmpty(message))
            {
                var parameters = info.GetParametersFromEventId(eventId);
                Debug.Assert(parameters.Length == eventData.Payload.Count, "Even parameters array length should be the same as the payload collection");

                message = string.Join(", ", Enumerable.Range(0, parameters.Length).Select(n => string.Concat(parameters[n], "=", eventData.Payload[n])));
            }

            try
            {
                this.traceQueue.Add(string.Join(" ", timestamp, info.FriendlyName, eventData.Level, info.GetNameFromEventId(eventId), message));
            }
            catch (InvalidOperationException)
            {
                // The queue is completed adding, no more logs are accepted.
            }
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

        /// <summary>
        /// Thread to write traces to the log file
        /// </summary>
        /// <param name="param">cancellation token</param>
        private void WriteTraceToFile(object param)
        {
            var cancellationToken = (CancellationToken)param;

            StreamWriter file = null;
            int linefeedLength = 0;
            int fileSize = 0;

            if (!Directory.Exists(this.logDirectory))
            {
                Directory.CreateDirectory(this.logDirectory);
            }

            Directory.SetCurrentDirectory(this.logDirectory);

            long totalSize = DeleteOldestLogFiles(this.logDirectory, this.logDirectoryQuotaInBytes);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (file == null)
                    {
                        file = File.CreateText(this.createLogFileName());
                        linefeedLength = file.NewLine.Length;
                        file.WriteLine(this.traceHeader);
                        fileSize = this.traceHeader.Length + linefeedLength;
                    }

                    try
                    {
                        string line;

                        if (!this.traceQueue.TryTake(out line, LogFileFlushInterval, cancellationToken))
                        {
                            file.Flush();
                            continue;
                        }

                        file.WriteLine(line);
                        fileSize += line.Length + linefeedLength;
                    }
                    catch (IOException)
                    {
                        // I/O error occurred during write. Close the file and start cleanup, hopefully the problem can be recovered.
                        fileSize = int.MaxValue;
                    }
                    catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
                    {
                        // Process is terminating, write whatever left as soon as possible and get out of this thread.
                        file.AutoFlush = true;
                        file.WriteLine("### Logging flushing...");

                        string line;
                        while (this.traceQueue.TryTake(out line, 0))
                        {
                            file.WriteLine(line);
                        }

                        file.WriteLine("### Logging stopped.");

                        file.Close();
                        file = null;

                        break;
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
            }
            finally
            {
                file?.Dispose();
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
            private readonly Dictionary<int, Tuple<string, string[]>> events = new Dictionary<int, Tuple<string, string[]>>();

            /// <summary>
            /// Gets the friendly name of the event source
            /// </summary>
            public string FriendlyName { get; private set; }

            /// <summary>
            /// Parses the <see cref="EventSource"/> class and returns the <see cref="EventSourceInfo"/> object
            /// </summary>
            /// <param name="eventSource">Event source to be parsed</param>
            /// <param name="friendlyName">Friendly name of the event source</param>
            /// <returns>event source info</returns>
            public static EventSourceInfo Parse(EventSource eventSource, string friendlyName)
            {
                if (string.IsNullOrEmpty(friendlyName))
                {
                    friendlyName = eventSource.Name;
                }

                var info = new EventSourceInfo { FriendlyName = friendlyName, };

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

                    info.events.Add(id, Tuple.Create(name, parameters));
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
                return this.events[id].Item1;
            }

            /// <summary>
            /// Gets the list of parameters of the specified event ID
            /// </summary>
            /// <param name="id">Event ID</param>
            /// <returns>List of parameters</returns>
            public string[] GetParametersFromEventId(int id)
            {
                return this.events[id].Item2;
            }
        }
    }
}
