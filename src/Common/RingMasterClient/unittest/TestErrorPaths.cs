// <copyright file="TestErrorPaths.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterClientUnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify that the RingMaster APIs handle errors properly.
    /// </summary>
    [TestClass]
    public class TestErrorPaths : RingMasterClientUnitTest
    {
        /// <summary>
        /// Implementation of error path test cases.
        /// </summary>
        private readonly RingMaster.TestCases.TestErrorPaths errorPathTests = new RingMaster.TestCases.TestErrorPaths();

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.errorPathTests.ConnectToRingMaster = this.ConnectToRingMaster;
            this.SetupTest();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Create"/> method is
        /// used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestCreateErrorPaths()
        {
            this.errorPathTests.TestCreateErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Delete"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestDeleteErrorPaths()
        {
            this.errorPathTests.TestDeleteErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Exists"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestExistsErrorPaths()
        {
            this.errorPathTests.TestExistsErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="GetChildren"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        [Ignore]
        public void TestGetChildrenErrorPaths()
        {
            this.errorPathTests.TestGetChildrenErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="GetData"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestGetDataErrorPaths()
        {
            this.errorPathTests.TestGetDataErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="SetData"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestSetDataErrorPaths()
        {
            this.errorPathTests.TestSetDataErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="GetACL"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestGetACLErrorPaths()
        {
            this.errorPathTests.TestGetACLErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="SetACL"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestSetACLErrorPaths()
        {
            this.errorPathTests.TestSetACLErrorPaths().Wait();
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Sync"/> method
        /// is used incorrectly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestSyncErrorPaths()
        {
            this.errorPathTests.TestSyncErrorPaths().Wait();
        }

        /// <summary>
        /// Verifies that an attempt to GetData from a non-existent node through a Multi operation
        /// correctly returns <c>Nonode</c> error.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestMultiGetDataFromSingleNonExistentNode()
        {
            this.errorPathTests.TestMultiGetDataFromSingleNonExistentNode().Wait();
        }

        /// <summary>
        /// Verifies that an attempt to GetData from many non-existent nodes through a Multi operation
        /// correctly returns one <c>Nonode</c> error.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestMultiGetDataFromManyNonExistentNodes()
        {
            this.errorPathTests.TestMultiGetDataFromManyNonExistentNodes().Wait();
        }

        /// <summary>
        /// Verifies that an attempt to GetData from some non-existent nodes and some existing nodes
        /// through a Multi operation correctly returns results until the first non-existing node.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestMultiGetDataFromSomeNonExistentNodes()
        {
            this.errorPathTests.TestMultiGetDataFromSomeNonExistentNodes().Wait();
        }
    }
}