// <copyright file="TestFunctionality.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify ringmaster functionality.
    /// </summary>
    [TestClass]
    public class TestFunctionality : RingMasterBackendCoreUnitTest
    {
        private readonly Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases.TestFunctionality functionalityTest;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFunctionality"/> class.
        /// </summary>
        public TestFunctionality()
        {
            this.functionalityTest = new Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases.TestFunctionality();
        }

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.functionalityTest.ConnectToRingMaster = this.ConnectToRingMaster;
            this.functionalityTest.Initialize();
        }

        /// <summary>
        /// Cleanups this instance.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
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
        /// Verifies CreateMode.Ephemeral | CreateMode.AllowPathCreationFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralNode_AllowPathCreationFlag()
        {
            this.functionalityTest.TestCreateEphemeralNode_AllowPathCreationFlag().Wait();
        }

        /// <summary>
        /// Verifies CreateMode.Ephemeral | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralNode_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreateEphemeralNode_SuccessEvenIfNodeExistsFlag().Wait();
        }

        /// <summary>
        /// Verifies CreateMode.Ephemeral | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreateEphemeralNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag().Wait();
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
        /// Verifies CreateMode.EphemeralSequential | CreateMode.AllowPathCreationFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralSequentialNode_AllowPathCreationFlag()
        {
            this.functionalityTest.TestCreateEphemeralSequentialNode_AllowPathCreationFlag().Wait();
        }

        /// <summary>
        /// Verifies CreateMode.EphemeralSequential | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralSequentialNode_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreateEphemeralSequentialNode_SuccessEvenIfNodeExistsFlag().Wait();
        }

        /// <summary>
        /// Verifies CreateMode.EphemeralSequential | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateEphemeralSequentialNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreateEphemeralSequentialNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag().Wait();
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
        /// Verify CreateMode.Persistent | CreateMode.AllowPathCreationFlag
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentNode_AllowPathCreationFlag()
        {
            this.functionalityTest.TestCreatePersistentNode_AllowPathCreationFlag().Wait();
        }

        /// <summary>
        /// Verify CreateMode.Persistent | CreateMode.SuccessEvenIfNodeExistsFlag
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentNode_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreatePersistentNode_SuccessEvenIfNodeExistsFlag().Wait();
        }

        /// <summary>
        /// Verify CreateMode.Persistent | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreatePersistentNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag().Wait();
        }

        /// <summary>
        /// Verifies that Persistent nodes can be moved
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestMovePersistentNode()
        {
            this.functionalityTest.TestMovePersistentNode().Wait();
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
        /// Verifies CreateMode.PersistentSequential | CreateMode.AllowPathCreationFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentSequentialNode_AllowPathCreationFlag()
        {
            this.functionalityTest.TestCreatePersistentSequentialNode_AllowPathCreationFlag().Wait();
        }

        /// <summary>
        /// Verifies CreateMode.PersistentSequential | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentSequentialNode_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreatePersistentSequentialNode_SuccessEvenIfNodeExistsFlag().Wait();
        }

        /// <summary>
        /// Verifies CreateMode.PersistentSequential | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreatePersistentSequentialNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            this.functionalityTest.TestCreatePersistentSequentialNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag().Wait();
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
        /// Verify that a node can be deleted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDelete()
        {
            this.functionalityTest.TestDelete().Wait();
        }

        /// <summary>
        /// Verify that a node can be fast-deleted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestFastDelete()
        {
            this.functionalityTest.TestCascadeDelete(fastDelete: true).Wait();
        }

        /// <summary>
        /// Verify that a node can be fast-deleted.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCascadeDelete()
        {
            this.functionalityTest.TestCascadeDelete(fastDelete: false).Wait();
        }

        /// <summary>
        /// Verify that a node can be deleted and also validates the allow node not exist flag.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteWithAllowNonExistFlag()
        {
            this.functionalityTest.TestDeleteWithAllowNonExistFlag().Wait();
        }

        /// <summary>
        /// Verify that Retrieval Conditions can be specified to the GetChildren method to
        /// select the children to return.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetChildrenRetrievalConditions()
        {
            this.functionalityTest.TestGetChildrenRetrievalConditions().Wait();
        }

        /// <summary>
        /// Verify that Retrieval Conditions must be specified to the GetChildren method to
        /// select a subset of children from a node that has large number of children.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetChildrenRetrievalConditions_NodeWithLargeNumberOfChildren()
        {
            this.functionalityTest.TestGetChildrenRetrievalConditions_NodeWithLargeNumberOfChildren().Wait();
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
        /// Verify that (if the node exists and the allow-existing-node-flag is set)
        /// data to be associated with nodes can be provided at creation time
        /// and that data can be retrieved using the GetData method.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateNodeWithDataOnExistingNode()
        {
            this.functionalityTest.TestCreateNodeWithDataOnExistingNode().Wait();
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
            this.functionalityTest.TestCreateLargeData(256 * 1024).Wait();
        }

        /// <summary>
        /// Verify that data associated with a sub tree can be retrieved using the <c>path/$fullsubtree$</c>
        /// syntax.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetFullSubtreeData()
        {
            this.functionalityTest.TestGetFullSubtreeData().Wait();
        }

        /// <summary>
        /// Verifies that GetData operation on a node that has no data with <c>FaultbackOnParentData</c> option
        /// retrieves no data.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetDataFaultbackOnParentData()
        {
            this.functionalityTest.TestGetDataFaultbackOnParentData().Wait();
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
        /// Verifies that GetData operation on a non-existent node with <c>FaultbackOnParentData</c> option
        /// returns Nonode error.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetDataFaultbackOnAncestorData()
        {
            this.functionalityTest.TestGetDataFaultbackOnAncestorData().Wait();
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

        /// <summary>
        /// Verifies that Multi operation works correctly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestMultiScheduled()
        {
            this.StartPseudoNodes();
            try
            {
                this.functionalityTest.TestMultiScheduled().Wait();
            }
            finally
            {
                this.StopPseudoNodes();
            }
        }

        /// <summary>
        /// Verifies that lock collision is handled
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetLockCollisionInMulti()
        {
            this.functionalityTest.GetLockCollisionInMulti().Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestGetSubtreeNoStats()
        {
            this.functionalityTest.TestGetSubtree(false).Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestGetSubtreeWithStats()
        {
            this.functionalityTest.TestGetSubtree(true).Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestGetSubtreeContinuationContainsNonExistentPath()
        {
            this.functionalityTest.TestGetSubtreeContinuationContainsNonExistentPath().Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestGetSubtreeContinuationPathEqualsNodePath()
        {
            this.functionalityTest.TestGetSubtreeContinuationPathEqualsNodePath().Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestGetSubtreeOfRoot()
        {
            this.functionalityTest.TestGetSubtreeOfRoot().Wait();
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestExistsSyncGetSubtreeBatch()
        {
            this.functionalityTest.TestExistsSyncGetSubtreeBatch().Wait();
        }
    }
}