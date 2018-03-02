// <copyright file="TestAccessControl.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify that access control lists on nodes are handled correctly.
    /// </summary>
    [TestClass]
    public class TestAccessControl : RingMasterBackendCoreUnitTest
    {
        /// <summary>
        /// Implementation of access control test cases.
        /// </summary>
        private readonly RingMaster.TestCases.TestAccessControl accessControlTests = new RingMaster.TestCases.TestAccessControl();

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.accessControlTests.ConnectToRingMaster = this.ConnectToRingMaster;
            this.accessControlTests.Initialize();
        }

        /// <summary>
        /// Verify that if a node has no access control settings, then clients with any identity can access it.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestNoAccessControl()
        {
            this.accessControlTests.TestNoAccessControl().Wait();
        }

        /// <summary>
        /// Verify that the ACLs on a node are honored for creates.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestCreateAccessControl()
        {
            this.accessControlTests.TestCreateAccessControl().Wait();
        }

        /// <summary>
        /// Verify that the ACLs on a node are honored for operations.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestOperationAccessControl()
        {
            this.accessControlTests.TestOperationAccessControl().Wait();
        }

        /// <summary>
        /// Verify that the ACLs on a node can be changed.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestChangeAccessControl()
        {
            this.accessControlTests.TestChangeAccessControl().Wait();
        }

        /// <summary>
        /// Verify that the ACLs on a node can be removed.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestRemoveAccessControl()
        {
            this.accessControlTests.TestRemoveAccessControl().Wait();
        }

        /// <summary>
        /// Verify that ACL is enforced for read requests sent in a batch
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestBatchReadAccessControl()
        {
            this.accessControlTests.TestBatchReadAccessControl().Wait();
        }
    }
}