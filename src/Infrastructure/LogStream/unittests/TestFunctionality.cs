// <copyright file="TestFunctionality.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.LogStreamUnitTest
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using FluentAssertions;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.LogStream;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify <see cref="LogStream"/> functionality.
    /// </summary>
    [TestClass]
    public class TestFunctionality
    {
        /// <summary>
        /// Prefix that identifies files created by this test.
        /// </summary>
        private const string Prefix = "LogStreamUnitTestFile";

        /// <summary>
        /// Extension used to mark completed files.
        /// </summary>
        private const string CompletedExtension = ".completed";

        /// <summary>
        /// Extension used to mark files that are in progress.
        /// </summary>
        private const string InprogressExtension = ".inprogress";

        /// <summary>
        /// Path that will be used for writing test files.
        /// </summary>
        private string testPath;

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.testPath = Path.Combine(Path.GetTempPath(), "LogStreamUnitTest");
            if (Directory.Exists(this.testPath))
            {
                Directory.Delete(this.testPath, recursive: true);
            }

            Directory.CreateDirectory(this.testPath);
        }

        /// <summary>
        /// Cleans up after the test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(this.testPath, recursive: true);
        }

        /// <summary>
        /// Verify that entries are written and the underlying file is completed if log stream is stopped.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestStop()
        {
            var instrumentation = new LogStreamInstrumentation();
            using (var logStream = this.CreateLogStream(instrumentation))
            {
                logStream.Start();

                logStream.Write(0, "Hello");
                logStream.Write(1, "World");

                logStream.Stop();

                instrumentation.OnEntryWrittenCount.Should().Be(2);
                instrumentation.OnFileCompletedCount.Should().Be(1);
            }
        }

        /// <summary>
        /// Verify that entries are written and the underlying file is completed if log stream is disposed.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestDispose()
        {
            var instrumentation = new LogStreamInstrumentation();
            using (var logStream = this.CreateLogStream(instrumentation))
            {
                logStream.Start();

                logStream.Write(0, "Hello");
                logStream.Write(1, "World");
            }

            instrumentation.OnEntryWrittenCount.Should().Be(2);
            instrumentation.OnFileCompletedCount.Should().Be(1);
        }

        /// <summary>
        /// Verify that Write works correctly if the log stream is never started.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestWriteWithoutStart()
        {
            var instrumentation = new LogStreamInstrumentation();
            using (var logStream = this.CreateLogStream(instrumentation))
            {
                logStream.Write(0, "Hello");
            }

            instrumentation.OnEntryWrittenCount.Should().Be(0);
            instrumentation.OnFileCompletedCount.Should().Be(0);
        }

        /// <summary>
        /// Verify that Write works correctly if the log stream has been stopped.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestWriteAfterStop()
        {
            var instrumentation = new LogStreamInstrumentation();
            using (var logStream = this.CreateLogStream(instrumentation))
            {
                logStream.Start();

                var entryWritten = new ManualResetEventSlim();
                var fileCompleted = new ManualResetEventSlim();

                instrumentation.EntryWritten = sequenceNumber => entryWritten.Set();
                instrumentation.FileCompleted = sequenceNumber => fileCompleted.Set();

                logStream.Write(0, "Hello");
                entryWritten.Wait();

                logStream.Stop();
                fileCompleted.Wait();

                logStream.Invoking(ls => ls.Write(1, "World")).ShouldThrow<InvalidOperationException>();
            }

            instrumentation.OnEntryWrittenCount.Should().Be(1);
            instrumentation.OnFileCompletedCount.Should().Be(1);
        }

        /// <summary>
        /// Verify that Dispose works correctly if the log stream is never started.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestDisposeWithoutStart()
        {
            var instrumentation = new LogStreamInstrumentation();
            using (var logStream = this.CreateLogStream(instrumentation))
            {
            }

            instrumentation.OnEntryWrittenCount.Should().Be(0);
            instrumentation.OnFileCompletedCount.Should().Be(0);
        }

        /// <summary>
        /// Verify that <see cref="LogStream"/> observes the cancellation token.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestCancel()
        {
            using (var cancellationSource = new CancellationTokenSource())
            {
                var instrumentation = new LogStreamInstrumentation();
                using (var logStream = this.CreateLogStream(instrumentation, cancellationSource.Token))
                {
                    logStream.Start();

                    var entryWritten = new ManualResetEventSlim();
                    var fileCompleted = new ManualResetEventSlim();

                    instrumentation.EntryWritten = sequenceNumber => entryWritten.Set();
                    instrumentation.FileCompleted = sequenceNumber => fileCompleted.Set();

                    logStream.Write(0, "Hello");
                    entryWritten.Wait();

                    logStream.Write(1, "World");
                    cancellationSource.Cancel();
                    fileCompleted.Wait();
                }

                instrumentation.OnFileCompletedCount.Should().Be(1);
            }
        }

        /// <summary>
        /// Verify that MaxEntriesPerFile setting is used to determine when to close the current file
        /// and start a new one.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestMaxEntriesPerFile()
        {
            var instrumentation = new LogStreamInstrumentation();
            using (var logStream = this.CreateLogStream(instrumentation))
            {
                logStream.MinEntriesPerFile = 5;
                logStream.MaxEntriesPerFile = 10;
                logStream.Start();

                for (ulong i = 0; i < 50; i++)
                {
                    logStream.Write(i, "Test");
                }
            }

            instrumentation.OnEntryWrittenCount.Should().Be(50);
            instrumentation.OnFileCompletedCount.Should().Be(5);
        }

        /// <summary>
        /// Verify that MinEntriesPerFile setting is used to determine when to close the current file
        /// and start a new one when the incoming rate is too slow.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void TestMinEntriesPerFile()
        {
            var instrumentation = new LogStreamInstrumentation();
            using (var logStream = this.CreateLogStream(instrumentation))
            {
                logStream.MinEntriesPerFile = 5;
                logStream.MaxEntriesPerFile = 10;
                logStream.MaxFileLifeSpan = TimeSpan.FromSeconds(1);
                logStream.Start();

                for (ulong i = 0; i < 15; i++)
                {
                    logStream.Write(i, "Test");
                    Thread.Sleep(200);
                }
            }

            instrumentation.OnEntryWrittenCount.Should().Be(15);
            instrumentation.OnFileCompletedCount.Should().Be(3);
        }

        /// <summary>
        /// Create a <see cref="LogStream"/> configured for use in this unit test.
        /// </summary>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token that must be observed for cancellation signal</param>
        /// <returns>A new instance of the <see cref="LogStream"/> class</returns>
        private LogStream CreateLogStream(LogStreamInstrumentation instrumentation, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new LogStream(this.testPath, Prefix, CompletedExtension, InprogressExtension, instrumentation, cancellationToken);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is a simple class that consumes instrumentation produced by LogStream")]
        private sealed class LogStreamInstrumentation : LogStream.IInstrumentation
        {
            public int OnEntryWrittenCount { get; private set; }

            public int OnFileCompletedCount { get; private set; }

            public Action<ulong> EntryWritten { get; set; }

            public Action<ulong> FileCompleted { get; set; }

            public void OnEntryWritten(ulong sequenceNumber)
            {
                this.OnEntryWrittenCount++;
                Trace.TraceInformation("OnEntryWritten sequenceNumber={0}", sequenceNumber);
                if (this.EntryWritten != null)
                {
                    this.EntryWritten(sequenceNumber);
                }
            }

            public void OnFileCompleted(ulong sequenceNumber)
            {
                this.OnFileCompletedCount++;
                Trace.TraceInformation("OnFileCompleted sequenceNumber={0}", sequenceNumber);
                if (this.FileCompleted != null)
                {
                    this.FileCompleted(sequenceNumber);
                }
            }
        }
    }
}
