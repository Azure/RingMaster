// <copyright file="LogStream.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.LogStream
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// LogStream represents an infinite sequence of entries ordered by a sequence number.
    /// </summary>
    public sealed class LogStream : IDisposable
    {
        /// <summary>
        /// Default instrumentation consumer.
        /// </summary>
        private static readonly IInstrumentation DefaultInstrumentationConsumer = new NoInstrumentation();

        /// <summary>
        /// Prefix of the file names created by this class.
        /// </summary>
        private readonly string fileNamePrefix;

        /// <summary>
        /// Extension that must be used for a file that has been completed.
        /// </summary>
        private readonly string fileCompletedExtension;

        /// <summary>
        /// Extension that must be used for a file that is in progress.
        /// </summary>
        private readonly string fileInProgressExtension;

        /// <summary>
        /// Token that will be observed for cancellation signal.
        /// </summary>
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Thread that writes queued messages to the file.
        /// </summary>
        private readonly Thread writerThread;

        /// <summary>
        /// Path where the log files will be stored.
        /// </summary>
        private readonly string path;

        /// <summary>
        /// Lifetime of the currently open stream.
        /// </summary>
        private readonly Stopwatch currentStreamLifetime = Stopwatch.StartNew();

        /// <summary>
        /// Queue from which entries are written to the current file.
        /// </summary>
        private readonly BlockingCollection<Entry> queue = new BlockingCollection<Entry>();

        /// <summary>
        /// Interface to the consumer of log stream metrics.
        /// </summary>
        private readonly IInstrumentation instrumentation;

        /// <summary>
        /// Path of the currently open file.
        /// </summary>
        private string currentFilePath;

        /// <summary>
        /// Currently open stream writer.
        /// </summary>
        private StreamWriter currentStream;

        /// <summary>
        /// The last sequence number that was written to a file.
        /// </summary>
        private ulong lastWrittenSequenceNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogStream" /> class.
        /// </summary>
        /// <param name="path">Path where the files will be stored</param>
        /// <param name="fileNamePrefix">Prefix of the file names</param>
        /// <param name="fileCompletedExtension">Extension that must be used for completed files</param>
        /// <param name="fileInProgressExtension">Extension that must be used for in progress files</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token that is observed for cancellation signal</param>
        public LogStream(string path, string fileNamePrefix, string fileCompletedExtension, string fileInProgressExtension, IInstrumentation instrumentation, CancellationToken cancellationToken)
        {
            this.path = path;
            this.fileNamePrefix = fileNamePrefix;
            this.fileCompletedExtension = fileCompletedExtension;
            this.fileInProgressExtension = fileInProgressExtension;
            this.instrumentation = instrumentation ?? DefaultInstrumentationConsumer;
            this.cancellationToken = cancellationToken;
            this.writerThread = new Thread(this.WriterThread);

            this.MaxEntriesPerFile = 100000;
            this.MinEntriesPerFile = 10000;
            this.MaxFileLifeSpan = TimeSpan.FromHours(6);
        }

        /// <summary>
        /// Interface that is used by the <see cref="LogStream"/> to report performance
        /// data and statistics.
        /// </summary>
        public interface IInstrumentation
        {
            /// <summary>
            /// An entry was written to the underlying file.
            /// </summary>
            /// <param name="sequenceNumber">Sequence number of the entry that was written</param>
            void OnEntryWritten(ulong sequenceNumber);

            /// <summary>
            /// A file was completed.
            /// </summary>
            /// <param name="sequenceNumber">Sequence number of the last entry that was written to the completed file</param>
            void OnFileCompleted(ulong sequenceNumber);
        }

        /// <summary>
        /// Gets or sets the maximum number of entries that can be stored in a single file.
        /// </summary>
        public ulong MaxEntriesPerFile { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of entries that must be in a file before it is considered
        /// for automatic closure.
        /// </summary>
        public ulong MinEntriesPerFile { get; set; }

        /// <summary>
        /// Gets or sets the amount of time that a file must be open before it is considered for
        /// automatic closure.
        /// </summary>
        public TimeSpan MaxFileLifeSpan { get; set; }

        /// <summary>
        /// Start the log stream.
        /// </summary>
        public void Start()
        {
            // Search for any files that were left in the "in progress" state by a previous
            // instance and mark them as completed.
            string searchpattern = "*" + this.fileInProgressExtension;
            foreach (string filePath in Directory.GetFiles(this.path, searchpattern))
            {
                this.MarkAsCompleted(filePath);
            }

            this.writerThread.Start();
        }

        /// <summary>
        /// Write a new entry to the log stream
        /// </summary>
        /// <param name="sequenceNumber">Sequence number of the entry</param>
        /// <param name="content">Content associated with the entry</param>
        public void Write(ulong sequenceNumber, object content)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            this.queue.Add(new Entry(sequenceNumber, content));
        }

        /// <summary>
        /// Stop the log stream.
        /// </summary>
        public void Stop()
        {
            this.queue.CompleteAdding();
            if (this.writerThread.IsAlive)
            {
                this.writerThread.Join();
            }
        }

        /// <summary>
        /// Dispose resources used by the log stream.
        /// </summary>
        public void Dispose()
        {
            this.Stop();

            if (this.currentStream != null)
            {
                this.currentStream.Dispose();
                this.currentStream = null;
            }

            this.queue.Dispose();
        }

        /// <summary>
        /// Write the entries that have accumulated in the queue to the current file.
        /// </summary>
        private void WriterThread()
        {
            try
            {
                LogStreamEventSource.Log.WriterThreadStarted();
                foreach (Entry entry in this.queue.GetConsumingEnumerable(this.cancellationToken))
                {
                    this.Write(entry);
                    this.lastWrittenSequenceNumber = entry.SequenceNumber;
                }
            }
            catch (Exception ex)
            {
                LogStreamEventSource.Log.WriterThreadException(this.currentFilePath, ex.ToString());
            }

            if (this.currentStream != null)
            {
                this.currentStream.Close();
                this.MarkAsCompleted(this.currentFilePath);
                this.instrumentation.OnFileCompleted(this.lastWrittenSequenceNumber);
            }

            LogStreamEventSource.Log.WriterThreadTerminated();
        }

        /// <summary>
        /// Write the given entry.
        /// </summary>
        /// <param name="entry">Entry to write</param>
        private void Write(Entry entry)
        {
            if ((this.currentStream == null)
            || ((entry.SequenceNumber % this.MaxEntriesPerFile) == 0)
            || ((entry.SequenceNumber % this.MinEntriesPerFile == 0) && (this.currentStreamLifetime.Elapsed > this.MaxFileLifeSpan)))
            {
                this.CreateNewFile(entry.SequenceNumber);
            }

            this.currentStream.WriteLine(entry.Content.ToString());
            this.instrumentation.OnEntryWritten(entry.SequenceNumber);
        }

        /// <summary>
        /// Create a new file for sequence numbers starting with the given sequence number.
        /// </summary>
        /// <param name="sequenceNumber">Sequence number of the first entry in the new file</param>
        private void CreateNewFile(ulong sequenceNumber)
        {
            if (this.currentStream != null)
            {
                this.currentStream.Close();
                this.MarkAsCompleted(this.currentFilePath);
                this.instrumentation.OnFileCompleted(this.lastWrittenSequenceNumber);
            }

            this.currentStreamLifetime.Restart();
            string fileName = string.Format("{0}-{1}{2}", this.fileNamePrefix, sequenceNumber, this.fileInProgressExtension);
            this.currentFilePath = Path.Combine(this.path, fileName);
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(this.currentFilePath, FileMode.CreateNew);
                this.currentStream = new StreamWriter(fileStream);
                fileStream = null;
                LogStreamEventSource.Log.NewFileCreated(this.currentFilePath);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Mark the given file as completed, by changing its extension to the completed extension.
        /// </summary>
        /// <param name="filePath">Path to the file that must be marked as completed</param>
        private void MarkAsCompleted(string filePath)
        {
            try
            {
                string completedFilePath = Path.ChangeExtension(filePath, this.fileCompletedExtension);
                File.Move(filePath, completedFilePath);
                LogStreamEventSource.Log.FileMarkedAsCompleted(completedFilePath);
            }
            catch (Exception ex)
            {
                LogStreamEventSource.Log.MarkFileAsCompletedFailed(filePath, ex.ToString());
            }
        }

        /// <summary>
        /// Represents an individual entry that is stored in the log stream.
        /// </summary>
        private struct Entry
        {
            /// <summary>
            /// Sequence number of the entry.
            /// </summary>
            public ulong SequenceNumber;

            /// <summary>
            /// Content associated with the entry.
            /// </summary>
            public object Content;

            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> struct.
            /// </summary>
            /// <param name="sequenceNumber">Sequence number of the entry</param>
            /// <param name="content">Content associated with the entry</param>
            public Entry(ulong sequenceNumber, object content)
            {
                this.SequenceNumber = sequenceNumber;
                this.Content = content;
            }
        }

        /// <summary>
        /// Default Instrumentation consumer that does nothing.
        /// </summary>
        private class NoInstrumentation : IInstrumentation
        {
            /// <inheritdoc />
            public void OnEntryWritten(ulong sequenceNumber)
            {
            }

            /// <inheritdoc />
            public void OnFileCompleted(ulong sequenceNumber)
            {
            }
        }
    }
}
