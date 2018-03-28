// <copyright file="QueuedWorkItemPoolTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterCommonUnitTest
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Sanity check of <see cref="QueuedWorkItemPool"/> class
    /// </summary>
    [TestClass]
    public class QueuedWorkItemPoolTests
    {
        /// <summary>
        /// Gets or sets the test context.
        /// </summary>
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            QueuedWorkItemPool.Default.Initialize(10, CancellationToken.None);
        }

        [TestMethod]
        public void TestSeveralActions()
        {
            const int totalCount = 1000 * 1000 * 10;
            int count = 0;

            var stopwatch = Stopwatch.StartNew();
            Parallel.ForEach(
                Enumerable.Range(0, totalCount),
                n => QueuedWorkItemPool.Default.Queue(() => Interlocked.Increment(ref count)));

            SpinWait.SpinUntil(() => count == totalCount, 1000 * 60);
            stopwatch.Stop();

            Assert.AreEqual(totalCount, count, "All actions must be scheduled and completed in one minute");

            var duration = stopwatch.ElapsedMilliseconds;
            var rate = totalCount / duration * 1000.0;
            this.TestContext.WriteLine($"Duration for {count} actions: {duration:G4} ms, rate: {rate:G4} qps");
        }
    }
}
