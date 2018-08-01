// <copyright file="TestErrorPaths.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify that the RingMaster APIs handle errors properly.
    /// </summary>
    public sealed class TestErrorPaths : RingMasterInterfaceTest
    {
        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Create"/> method is
        /// used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                // RingMasterException code Nonode must be thrown if an attempt is made to create a new node
                // under a non-existent parent.
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        string pathWithNonExistentParent = string.Format("$bvt_TestCreateErrorPaths_nonexistent{0}/newnode", Guid.NewGuid());
                        await ringMaster.Create(pathWithNonExistentParent, null, null, CreateMode.Ephemeral);
                    },
                    "Nonode error if an attempt is made to create a node under a non-existent parent");

                // RingMasterException code Nonode must be thrown if an attempt is made to create a node
                // with an empty name.
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        await ringMaster.Create("/", null, null, CreateMode.Ephemeral);
                    },
                    "Nonode error if an attempt is made to create a node with an empty name");

                string ephemeralParentPath = string.Format("/$bvt_TestCreateErrorPaths_EphemeralParent{0}", Guid.NewGuid());
                await ringMaster.Create(ephemeralParentPath, null, null, CreateMode.Ephemeral);

                string ephemeralChildPath = ephemeralParentPath + "/child";

                // RingMasterException code Nochildrenforephemerals must be thrown if an attempt is made to create a child node
                // under an ephemeral node.
                await VerifyRingMasterException(
                    RingMasterException.Code.Nochildrenforephemerals,
                    async () =>
                    {
                        await ringMaster.Create(ephemeralChildPath, null, null, CreateMode.Ephemeral);
                    },
                    "Nochildrenforephemerals error if an attempt is made to create a child node under an ephemeral node");

                await VerifyRingMasterException(
                    RingMasterException.Code.Nodeexists,
                    async () =>
                    {
                        string duplicateNodePath = string.Format("/$bvt_TestCreateErrorPaths_DuplicateNode{0}", Guid.NewGuid());

                        await ringMaster.Create(duplicateNodePath, null, null, CreateMode.Ephemeral);

                        // Exception must be thrown if an attempt was made to create a node that already exists.
                        await ringMaster.Create(duplicateNodePath, null, null, CreateMode.Ephemeral);
                    },
                    "Nodeexists error if an attempt is made to create a node that already exists");
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Delete"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestDeleteErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                // Delete must return false if an attempt is made to delete a
                // non-existent node.
                string pathToNonExistentNode = string.Format("/$bvt_TestDeleteErrorPaths_nonexistent{0}", Guid.NewGuid());
                Assert.IsFalse(await ringMaster.Delete(pathToNonExistentNode, 0));

                string nodeToDelete = string.Format("/$bvt_TestDeleteErrorPaths_{0}", Guid.NewGuid());
                await ringMaster.Create(nodeToDelete, null, null, CreateMode.Ephemeral);
                var stat = await ringMaster.Exists(nodeToDelete, watcher: null);

                await VerifyRingMasterException(
                    RingMasterException.Code.Badversion,
                    async () =>
                    {
                        await ringMaster.Delete(nodeToDelete, stat.Version + 5);
                    },
                    "Badversion error if an attempt is made to delete an incorrect version of a node");

                string nodeWithChild = string.Format("/$bvt_TestDeleteErrorPaths_Parent{0}", Guid.NewGuid());
                await ringMaster.Create(nodeWithChild, null, null, CreateMode.Persistent);

                await ringMaster.Create(nodeWithChild + "/child", null, null, CreateMode.Ephemeral);

                IStat nodeWithChildStat = await ringMaster.Exists(nodeWithChild, watcher: null);
                Assert.AreEqual(1, nodeWithChildStat.NumChildren);

                await VerifyRingMasterException(
                    RingMasterException.Code.Notempty,
                    async () =>
                    {
                        await ringMaster.Delete(nodeWithChild, nodeWithChildStat.Version);
                    },
                    "Notempty error if an attempt is made to delete a node that has children");

                Assert.IsTrue(await ringMaster.Delete(nodeWithChild + "/child", 1));
                Assert.IsTrue(await ringMaster.Delete(nodeWithChild, nodeWithChildStat.Version));
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Exists"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestExistsErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string pathToNonExistentNode = string.Format("/$bvt_TestExistsErrorPaths_nonexistent{0}", Guid.NewGuid());
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        IStat stat = await ringMaster.Exists(pathToNonExistentNode, watcher: null);
                    },
                    "Nonode error if an attempt is made to call Exists on a node that does not exist");
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="GetChildren"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        [TestMethod]
        public async Task TestGetChildrenErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string pathToNonExistentNode = string.Format("/$bvt_TestGetChildrenErrorPaths_nonexistent{0}", Guid.NewGuid());
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        IReadOnlyList<string> children = await ringMaster.GetChildren(pathToNonExistentNode, watcher: null, retrievalCondition: null);
                    },
                    "Nonode error if an attempt is made to call GetChildren on a node that does not exist");

                string parentNode = string.Format("/$bvt_TestGetChildrenErrorPaths_parent{0}", Guid.NewGuid());
                string childNode = string.Format("{0}/child", parentNode);

                await ringMaster.Create(parentNode, null, null, CreateMode.Persistent);
                await ringMaster.Create(childNode, null, null, CreateMode.Ephemeral);

                IStat parentStat = await ringMaster.Exists(parentNode, watcher: null);
                IStat childStat = await ringMaster.Exists(childNode, watcher: null);

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        // Retrieval condition should be of the form >:[top]:[startingChildName] so @:1,10 is wrong
                        var children = await ringMaster.GetChildren(parentNode, watcher: null, retrievalCondition: "@:1,10");
                    },
                    "Badarguments error if the retrieval condition is @:1,10");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        // Retrieval condition should be of the form >:[top]:[startingChildName] so >: is wrong because top and startingChildName
                        // are not specified
                        var children = await ringMaster.GetChildren(parentNode, watcher: null, retrievalCondition: ">:");
                    },
                    "Badarguments error if the retrieval condition does not have top or startingChildName specified");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        // Retrieval condition should be of the form >:[top]:[startingChildName] so >:: is wrong because top is not specified
                        var children = await ringMaster.GetChildren(parentNode, watcher: null, retrievalCondition: ">::");
                    },
                    "Badarguments error if the retrieval condition does not have top specified");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        // Retrieval condition should be of the form >:[top]:[startingChildName] so >:1 is wrong because startingChildName
                        // is not specified
                        var children = await ringMaster.GetChildren(parentNode, watcher: null, retrievalCondition: ">:1");
                    },
                    "Badarguments error if the retrieval condition does not have startingChildName specified");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        // Retrieval condition should be of the form >:[top]:[startingChildName] so >:max:child is wrong because
                        // max is not an integer value.
                        var children = await ringMaster.GetChildren(parentNode, watcher: null, retrievalCondition: ">:max:child");
                    },
                    "Badarguments error if the retrieval condition has invalid top value");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        // >:-1:child is wrong because -1 is < 0
                        var children = await ringMaster.GetChildren(parentNode, watcher: null, retrievalCondition: ">:-1:child");
                    },
                    "Badarguments error if the retrieval condition has a top value that is negative");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        // >:2000000:child is wrong because 2000000 is greater than the limit (which by default is 10000)
                        var children = await ringMaster.GetChildren(parentNode, watcher: null, retrievalCondition: ">:2000000:child");
                    },
                    "Badarguments error if the retrieval condition has a top value that exceeds limit");

                Assert.IsTrue(await ringMaster.Delete(childNode, childStat.Version));
                Assert.IsTrue(await ringMaster.Delete(parentNode, parentStat.Version));
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="GetData"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetDataErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string pathToNonExistentNode = string.Format("/$bvt_TestGetDataErrorPaths_nonexistent{0}", Guid.NewGuid());
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        byte[] data = await ringMaster.GetData(pathToNonExistentNode, watcher: null);
                    },
                    "Nonode error if an attempt is made to call GetData on a node that does not exist");
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="SetData"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestSetDataErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string pathToNonExistentNode = string.Format("/$bvt_TestSetDataErrorPaths_nonexistent{0}", Guid.NewGuid());
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        await ringMaster.SetData(pathToNonExistentNode, Guid.NewGuid().ToByteArray(), 1);
                    },
                    "Nonode error if an attempt is made to call SetData on a node that does not exist");

                string nodePath = string.Format("/$bvt_TestSetDataErrorPaths_node{0}", Guid.NewGuid());
                await ringMaster.Create(nodePath, null, null, CreateMode.Ephemeral);

                IStat stat = await ringMaster.Exists(nodePath, watcher: null);

                await VerifyRingMasterException(
                    RingMasterException.Code.Badversion,
                    async () =>
                    {
                        await ringMaster.SetData(nodePath, Guid.NewGuid().ToByteArray(), stat.Version + 1);
                    },
                    "Badversion error if the wrong version number was provided");
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="GetACL"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetACLErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string pathToNonExistentNode = string.Format("/$bvt_TestGetACLErrorPaths_nonexistent{0}", Guid.NewGuid());
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        await ringMaster.GetACL(pathToNonExistentNode, new Stat());
                    },
                    "Nonode error if an attempt is made to call GetACL on a node that does not exist");

                string nodePath = string.Format("/$bvt_TestGetACLErrorPaths_node{0}", Guid.NewGuid());

                await ringMaster.Create(nodePath, null, null, CreateMode.Ephemeral);
                Stat stat = new Stat(await ringMaster.Exists(nodePath, watcher: null));

                int temp = stat.Aversion;
                stat.Aversion = temp + 1;

                await VerifyRingMasterException(
                    RingMasterException.Code.Badversion,
                    async () =>
                    {
                        await ringMaster.GetACL(nodePath, stat);
                    },
                    "Badversion error if Aversion is incorrect");

                stat.Aversion = temp;
                temp = stat.Version;
                stat.Version = temp + 1;

                await VerifyRingMasterException(
                    RingMasterException.Code.Badversion,
                    async () =>
                    {
                        await ringMaster.GetACL(nodePath, stat);
                    },
                    "Badversion error if Version is incorrect");

                stat.Version = temp;
                temp = stat.Cversion;
                stat.Cversion = temp + 1;

                await VerifyRingMasterException(
                    RingMasterException.Code.Badversion,
                    async () =>
                    {
                        await ringMaster.GetACL(nodePath, stat);
                    },
                    "Badversion error if Cversion is incorrect");

                // Sanity check
                stat.Cversion = temp;
                Assert.AreEqual(null, await ringMaster.GetACL(nodePath, stat));
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="SetACL"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestSetACLErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string pathToNonExistentNode = string.Format("/$bvt_TestSetACLErrorPaths_nonexistent{0}", Guid.NewGuid());
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        await ringMaster.SetACL(pathToNonExistentNode, null, 1);
                    },
                    "Nonode error if an attempt is made to call SetACL on a node that does not exist");

                string nodePath = string.Format("/$bvt_TestSetACLErrorPaths_node{0}", Guid.NewGuid());

                await ringMaster.Create(nodePath, null, null, CreateMode.Ephemeral);
                IStat stat = await ringMaster.Exists(nodePath, watcher: null);

                await VerifyRingMasterException(
                    RingMasterException.Code.Badversion,
                    async () =>
                    {
                        await ringMaster.SetACL(nodePath, null, stat.Version + 1);
                    },
                    "Badversion error if Version is incorrect");
            }
        }

        /// <summary>
        /// Verify that the appropriate exceptions are thrown if the <see cref="Sync"/> method
        /// is used incorrectly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestSyncErrorPaths()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string pathToNonExistentNode = string.Format("/$bvt_TestSyncErrorPaths_nonexistent{0}", Guid.NewGuid());
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        await ringMaster.Sync(pathToNonExistentNode);
                    },
                    "Nonode error if an attempt is made to call Sync on a node that does not exist");
            }
        }

        /// <summary>
        /// Verifies that an attempt to GetData from a non-existent node through a Multi operation
        /// correctly returns <c>Nonode</c> error.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestMultiGetDataFromSingleNonExistentNode()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                var operations = new List<Op>();

                operations.Add(Op.GetData(string.Format("/DoesNotExist_{0}", Guid.NewGuid()), RequestGetData.GetDataOptions.None, null));

                IReadOnlyList<OpResult> results = await ringMaster.Multi(operations);
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(OpCode.Error, results[0].ResultType);
                Assert.AreEqual(RingMasterException.Code.Nonode, results[0].ErrCode);
            }
        }

        /// <summary>
        /// Verifies that an attempt to GetData from many non-existent nodes through a Multi operation
        /// correctly returns one <c>Nonode</c> error.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestMultiGetDataFromManyNonExistentNodes()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                var operations = new List<Op>();

                operations.Add(Op.GetData(string.Format("/DoesNotExist1_{0}", Guid.NewGuid()), RequestGetData.GetDataOptions.FaultbackOnParentData, null));
                operations.Add(Op.GetData(string.Format("/DoesNotExist2_{0}", Guid.NewGuid()), RequestGetData.GetDataOptions.None, null));
                operations.Add(Op.GetData(string.Format("/DoesNotExist3_{0}", Guid.NewGuid()), RequestGetData.GetDataOptions.NoStatRequired, null));
                operations.Add(Op.GetData(string.Format("/DoesNotExist4_{0}", Guid.NewGuid()), RequestGetData.GetDataOptions.NoWildcardsForPath, null));

                IReadOnlyList<OpResult> results = await ringMaster.Multi(operations);
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(OpCode.Error, results[0].ResultType);
                Assert.AreEqual(RingMasterException.Code.Nonode, results[0].ErrCode);
            }
        }

        /// <summary>
        /// Verifies that an attempt to GetData from some non-existent nodes and some existing nodes
        /// through a Multi operation correctly returns results until the first non-existing node.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestMultiGetDataFromSomeNonExistentNodes()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                var operations = new List<Op>();

                operations.Add(Op.GetData("/", RequestGetData.GetDataOptions.None, null));
                operations.Add(Op.GetData("/", RequestGetData.GetDataOptions.None, null));
                operations.Add(Op.GetData(string.Format("/DoesNotExist1_{0}", Guid.NewGuid()), RequestGetData.GetDataOptions.None, null));
                operations.Add(Op.GetData("/", RequestGetData.GetDataOptions.None, null));
                operations.Add(Op.GetData(string.Format("/DoesNotExist2_{0}", Guid.NewGuid()), RequestGetData.GetDataOptions.None, null));

                IReadOnlyList<OpResult> results = await ringMaster.Multi(operations);
                Assert.AreEqual(3, results.Count);
                Assert.AreEqual(OpCode.GetData, results[0].ResultType);
                Assert.AreEqual(RingMasterException.Code.Ok, results[0].ErrCode);
                Assert.AreEqual(OpCode.GetData, results[1].ResultType);
                Assert.AreEqual(RingMasterException.Code.Ok, results[1].ErrCode);
                Assert.AreEqual(OpCode.Error, results[2].ResultType);
                Assert.AreEqual(RingMasterException.Code.Nonode, results[2].ErrCode);
            }
        }

        public async Task TestGetSubtreeInvalidRetrievalCondition()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree("/", null);
                    },
                    "Badarguments error if an attempt is made to GetSubtree with null retrieval condition");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree("/", string.Empty);
                    },
                    "Badarguments error if an attempt is made to GetSubtree with empty retrieval condition");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree("/", ":1:");
                    },
                    "Badarguments error if an attempt is made to GetSubtree with non-well-formed retrieval condition");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree("/", ">:-1:");
                    },
                    "Badarguments error if an attempt is made to GetSubtree with negative top number");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree("/", ">:1:foo");
                    },
                    "Badarguments error if an attempt is made to GetSubtree with non-well-formed path");

                // setup node for test
                var testNodePath = $"/$bvt_TestGetSubtreeInvalidRetrievalCondition_nonexistent{Guid.NewGuid()}";
                await ringMaster.Create(testNodePath, null, null, CreateMode.PersistentAllowPathCreation);

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree(testNodePath, ">:1:/c");
                    },
                    "Badarguments error if an attempt is made to GetSubtree if continuation path is not a suffix of the node path");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree(testNodePath, $">:1:{testNodePath}a/b");
                    },
                    "Badarguments error if an attempt is made to GetSubtree if continuation path is not a suffix of the node path");
            }
        }

        public async Task TestGetSubtreeNonExistentNode()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                await VerifyRingMasterException(
                    RingMasterException.Code.Nonode,
                    async () =>
                    {
                        await ringMaster.GetSubtree($"/{Guid.NewGuid()}", ">:1:");
                    },
                    "Nonode error if an attempt is made to GetSubtree on non-existent node");

                await VerifyRingMasterException(
                    RingMasterException.Code.Badarguments,
                    async () =>
                    {
                        await ringMaster.GetSubtree(null, ">:1:");
                    },
                    "Badarguments error if an attempt is made to GetSubtree on null path");
            }
        }
    }
}