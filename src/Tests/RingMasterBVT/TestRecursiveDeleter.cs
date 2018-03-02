// <copyright file="TestRecursiveDeleter.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.BVT
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify recursive delete functionality.
    /// </summary>
    [TestClass]
    public sealed class TestRecursiveDeleter : RingMasterBVT
    {
        /// <summary>
        /// Watcher test implementation.
        /// </summary>
        private readonly RingMaster.TestCases.TestRecursiveDeleter recursiveDeleterTest = new RingMaster.TestCases.TestRecursiveDeleter();

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.recursiveDeleterTest.ConnectToRingMaster = this.ConnectToRingMaster;
            this.recursiveDeleterTest.Initialize();
        }

        /// <summary>
        /// Verify that recursively deleting a non existent node fails.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteNonExistentNode()
        {
            this.recursiveDeleterTest.TestDeleteNonExistentNode().Wait();
        }

        /// <summary>
        /// Verify that recursively deleting a node that has no children deletes the node.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteEmptyNode()
        {
            this.recursiveDeleterTest.TestDeleteEmptyNode().Wait();
        }

        /// <summary>
        /// Verify that cancellation token is observed.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCancelDelete()
        {
            this.recursiveDeleterTest.TestCancelDelete().Wait();
        }

        /// <summary>
        /// Verify failure to delete a node is handled properly.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteNodeFailure()
        {
            this.recursiveDeleterTest.TestDeleteNodeFailure().Wait();
        }

        /// <summary>
        /// Verify recursively deleting a node that has children.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteNodeWithChildren()
        {
            this.recursiveDeleterTest.TestDeleteNodeWithChildren().Wait();
        }

        /// <summary>
        /// Verify recursively deleting a node that has tree of children.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteNodeWithChildTree()
        {
            this.recursiveDeleterTest.TestDeleteNodeWithChildTree().Wait();
        }

        /// <summary>
        /// Verify recursively deleting a node that has a lot of immediate children
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDeleteNodeWithManyChildren()
        {
            this.recursiveDeleterTest.TestDeleteNodeWithManyChildren().Wait();
        }
    }
}
