// <copyright file="TestExecutionQueue.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.BVT
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify that the RingMaster execution queue functionality.
    /// </summary>
    [TestClass]
    public class TestExecutionQueue : RingMasterBVT
    {
        /// <summary>
        /// Implementation of error path test cases.
        /// </summary>
        private readonly RingMaster.TestCases.TestExecutionQueue executionQueueTests = new RingMaster.TestCases.TestExecutionQueue();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecutionQueue"/> class.
        /// </summary>
        public TestExecutionQueue()
        {
            this.executionQueueTests.ConnectToRingMaster = this.ConnectToRingMaster;
        }

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.executionQueueTests.Initialize();
        }

        /// <summary>
        /// Verify that ringmaster correctly handles a batch sent to the Transaction Manager execution queue with just book keeping requests.
        /// </summary>
        [TestMethod]
        public void TestTransactionManagerExecutionQueue_EmptyBatch()
        {
            this.executionQueueTests.TestTransactionManagerExecutionQueue_EmptyBatch().Wait();
        }
    }
}