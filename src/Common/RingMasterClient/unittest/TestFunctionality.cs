// <copyright file="TestFunctionality.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterClientUnitTest
{
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify ringmaster functionality.
    /// </summary>
    [TestClass]
    public class TestFunctionality : RingMasterClientUnitTest
    {
        /// <summary>
        /// Implementation of functionality test cases.
        /// </summary>
        private readonly RingMaster.TestCases.TestFunctionality functionalityTest = new RingMaster.TestCases.TestFunctionality();

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.SetupTest();
            this.functionalityTest.ConnectToRingMaster = this.ConnectToRingMaster;
            this.functionalityTest.Initialize();
        }

        /// <summary>
        /// Verifies that Ephemeral nodes can be created and are automatically deleted after
        /// the session that created the node is closed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralNode()
        {
            this.functionalityTest.TestCreateEphemeralNode().Wait();
        }

        /// <summary>
        /// Verifies that EphemeralSequential nodes can be created and are automatically deleted after
        /// the session that created the node is closed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralSequentialNode()
        {
            this.functionalityTest.TestCreateEphemeralSequentialNode().Wait();
        }

        /// <summary>
        /// Verifies that Persistent nodes can be created, enumerated and deleted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentNode()
        {
            this.functionalityTest.TestCreatePersistentNode().Wait();
        }

        /// <summary>
        /// Verifies that PersistentSequential nodes can be created, enumerated and deleted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentSequentialNode()
        {
            this.functionalityTest.TestCreatePersistentSequentialNode().Wait();
        }

        /// <summary>
        /// Verify that child nodes can be created and enumerated.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateChildNode()
        {
            this.functionalityTest.TestCreateChildNode().Wait();
        }

        /// <summary>
        /// Verify that the node path can contain unicode characters.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateUnicodePath()
        {
            this.functionalityTest.TestCreateUnicodePath().Wait();
        }

        /// <summary>
        /// Verify that data sizes up to the limit are supported and the limit is enforced.
        /// </summary>
        [TestMethod]
        [Timeout(300000)]
        public void TestCreateLargeData()
        {
            this.functionalityTest.TestCreateLargeData(10 * 1024 * 1024).Wait();
        }

        /// <summary>
        /// Verify that a node can be deleted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDelete()
        {
            this.functionalityTest.TestDelete().Wait();
        }

        /// <summary>
        /// Verify that Retrieval Conditions can be specified to the GetChildren method to
        /// select the children to return.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        [Ignore]
        public void TestGetChildrenRetrievalConditions()
        {
            this.functionalityTest.TestGetChildrenRetrievalConditions().Wait();
        }

        /// <summary>
        /// Verify that data to be associated with nodes can be provided at creation time
        /// and that data can be retrieved using the GetData method.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateNodeWithData()
        {
            this.functionalityTest.TestCreateNodeWithData().Wait();
        }

        /// <summary>
        /// Verify that data associated with a sub tree can be retrieved using the <c>path/$fullsubtree$</c>
        /// syntax.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        [Ignore]
        public void TestGetFullSubtreeData()
        {
            this.functionalityTest.TestGetFullSubtreeData().Wait();
        }

        /// <summary>
        /// Verifies that GetData operation on a node that has no data with <c>FaultbackOnParentDataWithMatch</c> option
        /// retrieves parent node's data that matches the given argument.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetDataFaultbackOnParentDataWithMatch()
        {
            this.functionalityTest.TestGetDataFaultbackOnParentDataWithMatch().Wait();
        }

        /// <summary>
        /// Verifies that GetData operation on a non-existent node with <c>FaultbackOnParentDataWithMatch</c> option
        /// retrieves the last existing ancestor node's data that matches the given argument.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetDataFaultbackOnAncestorDataWithMatch()
        {
            this.functionalityTest.TestGetDataFaultbackOnAncestorDataWithMatch().Wait();
        }

        /// <summary>
        /// Verify that data to be associated with a node can be set after
        /// the node is created and that data can be retrieved using the GetData method.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSetData()
        {
            this.functionalityTest.TestSetData().Wait();
        }

        /// <summary>
        /// Verify that Access control list to be associated with nodes can be provided at
        /// creation time and that data can be retrieved using the GetACL method.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateNodeWithAcl()
        {
            this.functionalityTest.TestCreateNodeWithAcl().Wait();
        }

        /// <summary>
        /// Verify that access control list to be associated with a node can be set after
        /// the node is created and that access control list can be retrieved using the
        /// GetACL method.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSetAcl()
        {
            this.functionalityTest.TestSetAcl().Wait();
        }

        /// <summary>
        /// Verifies that Multi operation works correctly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestMulti()
        {
            this.functionalityTest.TestMulti().Wait();
        }
    }
}