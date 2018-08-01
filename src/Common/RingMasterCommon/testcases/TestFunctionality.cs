// <copyright file="TestFunctionality.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify ringmaster functionality.
    /// </summary>
    public sealed class TestFunctionality : RingMasterInterfaceTest
    {
        /// <summary>
        /// The prefix which this test will use to create nodes
        /// </summary>
        private const string TestPrefix = "/$rmbvt/TestFunctionality";

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFunctionality"/> class.
        /// </summary>
        public TestFunctionality()
        {
        }

        /// <summary>
        /// Initializes the test.
        /// </summary>
        public void Initialize()
        {
            Task.Run(async () =>
            {
                using (var ringMaster = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Initializing TestRoot {0}", TestFunctionality.TestPrefix);
                    try
                    {
                        await ringMaster.Create(TestFunctionality.TestPrefix, null, null, CreateMode.PersistentAllowPathCreation);
                    }
                    catch (RingMasterException ex)
                    {
                        Assert.AreEqual(RingMasterException.Code.Nodeexists, ex.ErrorCode);
                    }
                }
            }).Wait();
        }

        /// <summary>
        /// Verifies that Ephemeral nodes can be created and are automatically deleted after
        /// the session that created the node is closed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreateEphemeralNode()
        {
            string nodeName = string.Format("$bvt_TestCreateEphemeralNode{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
            return this.VerifyCreateEphemeralNode(nodeName, nodePath, CreateMode.Ephemeral);
        }

        /// <summary>
        /// Verifies CreateMode.Ephemeral | CreateMode.AllowPathCreationFlag.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateEphemeralNode_AllowPathCreationFlag()
        {
            string nodeName = string.Format("$bvt_TestCreateEphemeralNode_AllowPathCreationFlag{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/a/b/c/d/e/f/g/{1}", TestFunctionality.TestPrefix, nodeName);
            await VerifyRingMasterException(
                RingMasterException.Code.Badarguments,
                async () =>
                {
                    using (var ringMaster = this.ConnectToRingMaster())
                    {
                        await ringMaster.Create(nodePath, null, null, CreateMode.Ephemeral | CreateMode.AllowPathCreationFlag);
                    }
                },
                "CreateMode.AllowPathCreationFlag is not compatible with CreateMode.Ephemeral");
        }

        /// <summary>
        /// Verifies CreateMode.Ephemeral | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreateEphemeralNode_SuccessEvenIfNodeExistsFlag()
        {
            string nodeName = string.Format("$bvt_TestCreateEphemeralNode_SuccessEvenInfNodeExistsFlag{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
            return this.VerifyCreateEphemeralNode(nodeName, nodePath, CreateMode.Ephemeral | CreateMode.SuccessEvenIfNodeExistsFlag);
        }

        /// <summary>
        /// Verifies CreateMode.Ephemeral | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExsitsFlag.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateEphemeralNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            string nodeName = string.Format("$bvt_TestCreateEphemeralNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/a/b/c/d/e/f/g/{1}", TestFunctionality.TestPrefix, nodeName);
            await VerifyRingMasterException(
                RingMasterException.Code.Badarguments,
                async () =>
                {
                    using (var ringMaster = this.ConnectToRingMaster())
                    {
                        await ringMaster.Create(nodePath, null, null, CreateMode.Ephemeral | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag);
                    }
                },
                "CreateMode.AllowPathCreationFlag is not compatible with CreateMode.Ephemeral");
        }

        /// <summary>
        /// Verifies that EphemeralSequential nodes can be created and are automatically deleted after
        /// the session that created the node is closed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreateEphemeralSequentialNode()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreateEphemeralSequentialRoot{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            return this.VerifyCreateEphemeralSequentialNode(rootPath, null, CreateMode.EphemeralSequential);
        }

        /// <summary>
        /// Verifies CreateMode.EphemeralSequential | CreateMode.AllowPathCreationFlag.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateEphemeralSequentialNode_AllowPathCreationFlag()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreateEphemeralSequential_AllowPathCreationFlagRoot{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            await VerifyRingMasterException(
                RingMasterException.Code.Badarguments,
                async () =>
                {
                    using (var ringMaster = this.ConnectToRingMaster())
                    {
                        await ringMaster.Create($"{rootPath}/a/b/c/d/e/f/g/node", null, null, CreateMode.EphemeralSequential | CreateMode.AllowPathCreationFlag);
                    }
                },
                "CreateMode.AllowPathCreationFlag is not compatible with CreateMode.EphemeralSequential");
        }

        /// <summary>
        /// Verifies CreateMode.EphemeralSequential | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreateEphemeralSequentialNode_SuccessEvenIfNodeExistsFlag()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreateEphemeralSequential_SuccessEvenIfNodeExistsFlagRoot{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            return this.VerifyCreateEphemeralSequentialNode(rootPath, null, CreateMode.EphemeralSequential | CreateMode.SuccessEvenIfNodeExistsFlag);
        }

        /// <summary>
        /// Verifies CreateMode.EphemeralSequential | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateEphemeralSequentialNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreateEphemeralSequential_AllowPathCreationFlagRoot_SuccessEvenIfNodeExistsFlag{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            await VerifyRingMasterException(
               RingMasterException.Code.Badarguments,
               async () =>
               {
                   using (var ringMaster = this.ConnectToRingMaster())
                   {
                       await ringMaster.Create($"{rootPath}/a/b/c/d/e/f/g/node", null, null, CreateMode.EphemeralSequential | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag);
                   }
               },
               "CreateMode.AllowPathCreationFlag is not compatible with CreateMode.EphemeralSequential");
        }

        /// <summary>
        /// Verifies that Persistent nodes can be moved
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestMovePersistentNode()
        {
            string nodePathSrc;
            string nodePathDst;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                string parentNodePathSrc = string.Format("{0}/$bvt_TestMovePersistentNode{1}", TestFunctionality.TestPrefix, Guid.NewGuid());
                nodePathSrc = string.Format("{0}/src", parentNodePathSrc);
                nodePathDst = string.Format("{0}/dst", parentNodePathSrc);

                byte[] data = ASCIIEncoding.ASCII.GetBytes("helloSrc");
                byte[] dataDst = ASCIIEncoding.ASCII.GetBytes("helloDst");

                string createdNodeName = await ringMaster.Create(nodePathSrc, data, null, CreateMode.PersistentAllowPathCreation);
                Assert.AreEqual(createdNodeName, "src");
                Thread.Sleep(100);

                string createdNodeNameDst = await ringMaster.Create(nodePathDst, dataDst, null, CreateMode.PersistentAllowPathCreation);
                Assert.AreEqual(createdNodeNameDst, "dst");
                Thread.Sleep(100);

                // Persistent node created by a session must exist while the session
                // is still active.
                IStat stat = await ringMaster.Exists(nodePathSrc, watcher: null);
                IStat statDst = await ringMaster.Exists(nodePathDst, watcher: null);

                VerifyStatForFreshlyCreatedNode(stat, data.Length);
                VerifyStatForFreshlyCreatedNode(statDst, dataDst.Length);

                // Move .../src with wrong version and verify failure
                string movedPath = null;

                try
                {
                    movedPath = await ringMaster.Move(nodePathSrc, 132, nodePathDst, MoveMode.None);
                    Assert.Fail("bad version should have been returned");
                }
                catch (RingMasterException exception)
                {
                    Assert.AreEqual(RingMasterException.Code.Badversion, exception.ErrorCode);
                }

                // Move .../src into .../dst/fail, and verify failure
                try
                {
                    movedPath = await ringMaster.Move(nodePathSrc, stat.Version, nodePathDst + "/fail", MoveMode.None);
                    Assert.Fail("nonode should have been returned");
                }
                catch (RingMasterException exception)
                {
                    Assert.AreEqual(RingMasterException.Code.Nonode, exception.ErrorCode, "Nonode should be received");
                }

                // Validate that nothing has changed for the two nodes
                IStat stat2 = await ringMaster.Exists(nodePathSrc, watcher: null);
                IStat stat2Dst = await ringMaster.Exists(nodePathDst, watcher: null);

                Assert.IsTrue(stat.Equals(stat2));
                Assert.IsTrue(statDst.Equals(stat2Dst));

                // Move .../src into .../dst, and verify success
                movedPath = await ringMaster.Move(nodePathSrc, stat.Version, nodePathDst, MoveMode.None);
                Assert.AreEqual(movedPath, nodePathDst + "/src");

                // Validate that stats have moved properly
                stat2 = await ringMaster.Exists(nodePathDst + "/src", watcher: null);
                stat2Dst = await ringMaster.Exists(nodePathDst, watcher: null);

                Assert.IsTrue(stat.Equals(stat2));
                Assert.AreEqual(statDst.NumChildren + 1, stat2Dst.NumChildren);
                Assert.AreEqual(statDst.Cversion + 1, stat2Dst.Cversion);
                AssertEx.IsGreaterThan(stat2Dst.Pzxid, statDst.Pzxid);
                AssertEx.IsGreaterThan(stat2Dst.Mtime, statDst.Mtime);
                AssertEx.IsGreaterThan(stat2Dst.Mzxid, statDst.Mzxid);
                Assert.AreEqual(stat2Dst.Ctime, statDst.Ctime);
                Assert.AreEqual(statDst.Version, stat2Dst.Version);
                Assert.AreEqual(statDst.Aversion, stat2Dst.Aversion);
                Assert.AreEqual(statDst.DataLength, stat2Dst.DataLength);
                Assert.AreEqual(statDst.Czxid, stat2Dst.Czxid);

                // Validate that data is not lost
                byte[] data2 = await ringMaster.GetData(nodePathDst + "/src", watcher: null);
                byte[] data2Dst = await ringMaster.GetData(nodePathDst, watcher: null);

                VerifyBytesAreEqual(data, data2);
                VerifyBytesAreEqual(dataDst, data2Dst);

                // Move .../dst/src back into .../src, and check a success
                movedPath = await ringMaster.Move(nodePathDst + "/src", stat.Version, parentNodePathSrc, MoveMode.None);
                Assert.AreEqual(movedPath, nodePathSrc);

                // Validate that stats have moved properly
                stat2 = await ringMaster.Exists(nodePathSrc, watcher: null);
                stat2Dst = await ringMaster.Exists(nodePathDst, watcher: null);

                Assert.IsTrue(stat.Equals(stat2));
                Assert.AreEqual(statDst.NumChildren, stat2Dst.NumChildren);
                Assert.AreEqual(statDst.Cversion + 2, stat2Dst.Cversion);
                Assert.AreEqual(stat2Dst.Czxid, statDst.Czxid);
                AssertEx.IsGreaterThan(stat2Dst.Mtime, statDst.Mtime);
                AssertEx.IsGreaterThan(stat2Dst.Mzxid, statDst.Mzxid);
                AssertEx.IsGreaterThan(stat2Dst.Pzxid, statDst.Pzxid);
                Assert.AreEqual(stat2Dst.Ctime, statDst.Ctime);
                Assert.AreEqual(statDst.Version, stat2Dst.Version);
                Assert.AreEqual(statDst.Aversion, stat2Dst.Aversion);
                Assert.AreEqual(statDst.DataLength, stat2Dst.DataLength);

                // Validate that data is not lost
                data2 = await ringMaster.GetData(nodePathSrc, watcher: null);
                data2Dst = await ringMaster.GetData(nodePathDst, watcher: null);

                VerifyBytesAreEqual(data, data2);
                VerifyBytesAreEqual(dataDst, data2Dst);

                // Move .../src into .../dst/newpath/more, and verify success
                string nodePathDst0 = nodePathDst;
                nodePathDst = nodePathDst + "/newpath/more";
                movedPath = await ringMaster.Move(nodePathSrc, stat.Version, nodePathDst, MoveMode.AllowPathCreationFlag);
                Assert.AreEqual(movedPath, nodePathDst + "/src");

                // Validate that stats have moved properly
                stat2 = await ringMaster.Exists(nodePathDst + "/src", watcher: null);
                stat2Dst = await ringMaster.Exists(nodePathDst, watcher: null);

                Assert.IsTrue(stat.Equals(stat2));
                Assert.AreEqual(1, stat2Dst.NumChildren);
                Assert.AreEqual(2, stat2Dst.Cversion);
                AssertEx.IsGreaterThan(stat2Dst.Pzxid, stat2.Mzxid);
                AssertEx.IsGreaterThan(stat2Dst.Mtime, stat2.Mtime);
                AssertEx.IsGreaterThan(stat2Dst.Mzxid, stat2.Mzxid);
                AssertEx.IsGreaterThan(stat2Dst.Ctime, stat2.Mtime);
                AssertEx.IsGreaterThan(statDst.Czxid, stat2.Mzxid);
                Assert.AreEqual(1, stat2Dst.Aversion);
                Assert.AreEqual(0, stat2Dst.DataLength);

                // Validate that data is not lost
                data2 = await ringMaster.GetData(nodePathDst + "/src", watcher: null);
                data2Dst = await ringMaster.GetData(nodePathDst0, watcher: null);

                VerifyBytesAreEqual(data, data2);
                VerifyBytesAreEqual(dataDst, data2Dst);

                // Move .../dst/newpath into .../src, and verify failure (not empty)
                try
                {
                    movedPath = await ringMaster.Move(nodePathDst0 + "/newpath", -1, nodePathSrc, MoveMode.OnlyIfSourcePathIsEmpty);
                    Assert.Fail("notempty should have been returned");
                }
                catch (RingMasterException exception)
                {
                    Assert.AreEqual(RingMasterException.Code.Notempty, exception.ErrorCode, "Notempty should be received");
                }

                // Delete the node, finally.
                IStat finalStat = await ringMaster.Exists(parentNodePathSrc, null);

                bool ok = await ringMaster.Delete(parentNodePathSrc, finalStat.Version, DeleteMode.CascadeDelete);
                Assert.IsTrue(ok);
            }
        }

        /// <summary>
        /// Verifies that Persistent nodes can be created, enumerated and deleted.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentNode()
        {
            string nodeName = string.Format("$bvt_TestCreatePersistentNode{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
            return this.VerifyCreatePersistentNode(nodeName, nodePath, CreateMode.Persistent);
        }

        /// <summary>
        /// Verifies CreateMode.Persistent | CreateMode.AllowPathCreationFlag
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentNode_AllowPathCreationFlag()
        {
            string nodeName = string.Format("$bvt_TestCreatePersistentNode_AllowPathCreationFlag{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/a/b/c/d/e/f/g/{1}", TestFunctionality.TestPrefix, nodeName);
            return this.VerifyCreatePersistentNode(nodeName, nodePath, CreateMode.Persistent | CreateMode.AllowPathCreationFlag);
        }

        /// <summary>
        /// Verifies CreateMode.Persistent | CreateMode.SuccessEvenIfNodeExistsFlag
        /// </summary>
        /// <remarks>
        /// This is a repro for VSO 1204321: RingMasterBackendCore: If a persistent node is created with SuccessEvenIfNodeExistsFlag, an ephemeral node is created instead.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentNode_SuccessEvenIfNodeExistsFlag()
        {
            string nodeName = string.Format("$bvt_TestCreatePersistentNode_SuccessEvenIfNodeExistsFlag{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
            return this.VerifyCreatePersistentNode(nodeName, nodePath, CreateMode.Persistent | CreateMode.SuccessEvenIfNodeExistsFlag);
        }

        /// <summary>
        /// Verifies CreateMode.Persistent | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            string nodeName = string.Format("$bvt_TestCreatePersistentNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/a/b/c/d/e/f/g/{1}", TestFunctionality.TestPrefix, nodeName);
            return this.VerifyCreatePersistentNode(nodeName, nodePath, CreateMode.Persistent | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag);
        }

        /// <summary>
        /// Verifies that PersistentSequential nodes can be created, enumerated and deleted.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentSequentialNode()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreatePersistentSequentialRoot{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            return this.VerifyCreatePersistentSequentialNode(rootPath, null, CreateMode.PersistentSequential);
        }

        /// <summary>
        /// Verifies CreateMode.PersistentSequentialAllowPathCreation
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentSequentialNode_AllowPathCreationFlag()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreatePersistentSequential_AllowPathCreationFlagRoot{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            return this.VerifyCreatePersistentSequentialNode(rootPath, "a/b/c/d/e/f/g", CreateMode.PersistentSequential | CreateMode.AllowPathCreationFlag);
        }

        /// <summary>
        /// Verifies CreateMode.PersistentSequential | CreateMode.SuccessEvenIfNodeExistsFlag
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentSequentialNode_SuccessEvenIfNodeExistsFlag()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreatePersistentSequential_SuccessEvenIfNodeExistsFlagRoot{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            return this.VerifyCreatePersistentSequentialNode(rootPath, null, CreateMode.PersistentSequential | CreateMode.SuccessEvenIfNodeExistsFlag);
        }

        /// <summary>
        /// Verifies CreateMode.PersistentSequential | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public Task TestCreatePersistentSequentialNode_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlag()
        {
            string rootPath = string.Format(
                "{0}/$bvt_TestCreatePersistentSequential_AllowPathCreationFlag_SuccessEvenIfNodeExistsFlagRoot{1}",
                TestFunctionality.TestPrefix,
                Guid.NewGuid());

            return this.VerifyCreatePersistentSequentialNode(rootPath, "a/b/c/d/e/f", CreateMode.PersistentSequential | CreateMode.AllowPathCreationFlag | CreateMode.SuccessEvenIfNodeExistsFlag);
        }

        /// <summary>
        /// Verify that child nodes can be created and enumerated.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateChildNode()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string parentPath = string.Format(
                    "{0}/$bvt_TestCreateChildNode_Parent_{1}",
                    TestFunctionality.TestPrefix,
                    Guid.NewGuid());

                string ephemeralChildPath = parentPath + "/EphemeralChild";
                string persistentChildPath = parentPath + "/PersistentChild";

                await ringMaster.Create(parentPath, null, null, CreateMode.Persistent);
                await ringMaster.Create(ephemeralChildPath, null, null, CreateMode.Ephemeral);
                await ringMaster.Create(persistentChildPath, null, null, CreateMode.Persistent);

                IStat statParent = await ringMaster.Exists(parentPath, watcher: null);
                IStat statEphemeralChild = await ringMaster.Exists(ephemeralChildPath, watcher: null);
                IStat statPersistentChild = await ringMaster.Exists(persistentChildPath, watcher: null);

                VerifyStatForFreshlyCreatedNode(statEphemeralChild, expectedDataLength: 0, context: "statEphemeralChild");
                VerifyStatForFreshlyCreatedNode(statPersistentChild, expectedDataLength: 0, context: "statPersistentChild");

                // Parent node has just been modified so Czxid (create transaction id)
                // must not be equal to Mzxid (modify transaction id).
                Assert.AreNotEqual(statParent.Czxid, statParent.Mzxid);

                // Since children were added, Pzxid must not be
                // the same as Czxid.
                Assert.AreNotEqual(statParent.Czxid, statParent.Pzxid);

                // No Changes to data or ACL, so those versions must be 1.
                Assert.AreEqual(1, statParent.Version);
                Assert.AreEqual(1, statParent.Aversion);

                // There were two child related changes, so Cversion must be 3.
                Assert.AreEqual(3, statParent.Cversion);

                // There are two children and the node has no data.
                Assert.AreEqual(2, statParent.NumChildren);
                Assert.AreEqual(0, statParent.DataLength);

                IReadOnlyList<string> childrenNames = await ringMaster.GetChildren(parentPath, watcher: null);
                Assert.IsTrue(childrenNames.Contains("EphemeralChild"));
                Assert.IsTrue(childrenNames.Contains("PersistentChild"));

                Assert.IsTrue(await ringMaster.Delete(ephemeralChildPath, statEphemeralChild.Version));
                Assert.IsTrue(await ringMaster.Delete(persistentChildPath, statPersistentChild.Version));
                Assert.IsTrue(await ringMaster.Delete(parentPath, statParent.Version));
            }
        }

        /// <summary>
        /// Verify that Retrieval Conditions can be specified to the GetChildren method to
        /// select the children to return.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetChildrenRetrievalConditions()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string parentPath = string.Format(
                    "{0}/$bvt_TestGetChildrenRetrievalConditions_Parent_{1}",
                    TestFunctionality.TestPrefix,
                    Guid.NewGuid());

                string child0Path = parentPath + "/Child0";
                string child1Path = parentPath + "/Child1";
                string child2Path = parentPath + "/Child2";

                await ringMaster.Create(parentPath, null, null, CreateMode.Persistent);

                await ringMaster.Create(child0Path, null, null, CreateMode.Persistent);
                await ringMaster.Create(child1Path, null, null, CreateMode.Persistent);
                await ringMaster.Create(child2Path, null, null, CreateMode.Persistent);

                // If retrievalCondition is null, all children are returned
                IReadOnlyList<string> noRetrievalConditionsResult = await ringMaster.GetChildren(parentPath, watcher: null, retrievalCondition: null);
                Assert.AreEqual(3, noRetrievalConditionsResult.Count);
                Assert.AreEqual("Child0", noRetrievalConditionsResult[0]);
                Assert.AreEqual("Child1", noRetrievalConditionsResult[1]);
                Assert.AreEqual("Child2", noRetrievalConditionsResult[2]);

                // If retrievalCondition is empty, all children are returned
                IReadOnlyList<string> emptyRetrievalConditionsResult = await ringMaster.GetChildren(parentPath, watcher: null, retrievalCondition: string.Empty);
                Assert.AreEqual(3, emptyRetrievalConditionsResult.Count);
                Assert.AreEqual("Child0", emptyRetrievalConditionsResult[0]);
                Assert.AreEqual("Child1", emptyRetrievalConditionsResult[1]);
                Assert.AreEqual("Child2", emptyRetrievalConditionsResult[2]);

                // If retrieval condition is >:1:Child0 only the second child is returned
                IReadOnlyList<string> top1AfterChild0Result = await ringMaster.GetChildren(parentPath, watcher: null, retrievalCondition: ">:1:Child0");
                Assert.AreEqual(1, top1AfterChild0Result.Count);
                Assert.AreEqual("Child1", top1AfterChild0Result[0]);

                // If retrieval condition is >:2: only the first two children are returned
                IReadOnlyList<string> top2Children = await ringMaster.GetChildren(parentPath, watcher: null, retrievalCondition: ">:2:");
                Assert.AreEqual(2, top2Children.Count);
                Assert.AreEqual("Child0", top2Children[0]);
                Assert.AreEqual("Child1", top2Children[1]);

                // Cleanup
                IStat parentStat = await ringMaster.Exists(parentPath, watcher: null);
                IStat child0Stat = await ringMaster.Exists(child0Path, watcher: null);
                IStat child1Stat = await ringMaster.Exists(child1Path, watcher: null);
                IStat child2Stat = await ringMaster.Exists(child2Path, watcher: null);
                Assert.IsTrue(await ringMaster.Delete(child0Path, child0Stat.Version), "Child 0 deleted");
                Assert.IsTrue(await ringMaster.Delete(child1Path, child1Stat.Version), "Child 1 deleted");
                Assert.IsTrue(await ringMaster.Delete(child2Path, child2Stat.Version), "Child 2 deleted");
                Assert.IsTrue(await ringMaster.Delete(parentPath, parentStat.Version), "Parent deleted");
            }
        }

        /// <summary>
        /// Verify that Retrieval Conditions must be specified to the GetChildren method to
        /// select a subset of children from a node that has large number of children.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetChildrenRetrievalConditions_NodeWithLargeNumberOfChildren()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string parentPath = string.Format(
                    "{0}/$bvt_TestGetChildrenRetrievalConditions_Parent_{1}",
                    TestFunctionality.TestPrefix,
                    Guid.NewGuid());

                await ringMaster.Create(parentPath, null, null, CreateMode.Persistent);

                for (int i = 0; i < 500; i++)
                {
                    await ringMaster.Create($"{parentPath}/Child{i}", null, null, CreateMode.Persistent);
                }

                await VerifyRingMasterException(
                    RingMasterException.Code.TooManyChildren,
                    async () => await ringMaster.GetChildren(parentPath, watcher: null, retrievalCondition: null),
                    "Exception is thrown if no retrieval conditions are given");

                await VerifyRingMasterException(
                    RingMasterException.Code.TooManyChildren,
                    async () => await ringMaster.GetChildren(parentPath, watcher: null, retrievalCondition: string.Empty),
                    "Exception is thrown if retrieval conditions is an empty string");

                // If retrievalCondition is empty, all children are returned
                IReadOnlyList<string> emptyRetrievalConditionsResult = await ringMaster.GetChildren(parentPath, watcher: null, retrievalCondition: ">:3:Child490");
                Assert.AreEqual(3, emptyRetrievalConditionsResult.Count);
                Assert.AreEqual("Child491", emptyRetrievalConditionsResult[0]);
                Assert.AreEqual("Child492", emptyRetrievalConditionsResult[1]);
                Assert.AreEqual("Child493", emptyRetrievalConditionsResult[2]);

                // Cleanup
                Assert.IsTrue(await ringMaster.Delete(parentPath, -1, DeleteMode.CascadeDelete));
            }
        }

        /// <summary>
        /// Verify that data to be associated with nodes can be provided at creation time
        /// and that data can be retrieved using the GetData method.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateNodeWithData()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestCreateNodeWithData{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
                byte[] nodeData = Guid.NewGuid().ToByteArray();

                string createdNodeName = await ringMaster.Create(nodePath, nodeData, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat, expectedDataLength: nodeData.Length);

                byte[] retrievedNodeData = await ringMaster.GetData(nodePath, watcher: null);

                VerifyBytesAreEqual(nodeData, retrievedNodeData);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that (if the node exists and the allowexistingnodeflag is set)
        /// data to be associated with nodes can be provided at creation time
        /// and that data can be retrieved using the GetData method.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateNodeWithDataOnExistingNode()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestCreateNodeWithDataOnExistingNode{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
                byte[] nodeData = Guid.NewGuid().ToByteArray();
                byte[] nodeData2 = Guid.NewGuid().ToByteArray();

                string createdNodeName = await ringMaster.Create(nodePath, nodeData, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat, expectedDataLength: nodeData.Length);

                byte[] retrievedNodeData = await ringMaster.GetData(nodePath, watcher: null);

                VerifyBytesAreEqual(nodeData, retrievedNodeData);

                IStat stat1 = await ringMaster.Exists(nodePath, watcher: null);

                await VerifyRingMasterException(
                    RingMasterException.Code.Nodeexists,
                    async () =>
                    {
                        await ringMaster.Create(nodePath, nodeData2, null, CreateMode.Persistent);
                    },
                    "Nodeexists error if an attempt is made to create a node that already exists");

                byte[] retrievedNodeData2 = await ringMaster.GetData(nodePath, watcher: null);
                VerifyBytesAreEqual(nodeData, retrievedNodeData2);

                IStat stat2 = await ringMaster.Exists(nodePath, watcher: null);
                Assert.AreEqual(stat1.Version, stat2.Version);

                string name2 = await ringMaster.Create(nodePath, nodeData2, null, CreateMode.Persistent | CreateMode.SuccessEvenIfNodeExistsFlag);

                // Verify that the create returns null
                Assert.AreEqual(name2, null);

                retrievedNodeData2 = await ringMaster.GetData(nodePath, watcher: null);
                VerifyBytesAreEqual(nodeData2, retrievedNodeData2, "after second create we should see a new data");

                IStat stat3 = await ringMaster.Exists(nodePath, watcher: null);
                Assert.AreEqual(stat1.Version + 1, stat3.Version);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that the node path can contain unicode characters.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateUnicodePath()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                Guid id = Guid.NewGuid();

                // Create two node paths that differ in unicode characters and will result in the same set of bytes if ASCII encoding is used.
                // This repros an issue where two unicode paths were allowed to be created in the primary, but they ended up with the same name
                // in the secondaries.
                string nodeName1 = string.Format("$bvt_TestCreateUnicodePath-\u00AE\u017F\u01FF\u04FF\u26FF\uFB4F\U0001034F\U00020FFF\U000E01EF-{0}", id);

                // Differs from nodeName1 only non basic latin characters in the Basic Multilingual plane.
                string nodeName2 = string.Format("$bvt_TestCreateUnicodePath-\u00AD\u017E\u01FD\u04FF\u26FF\uFB4F\U0001034F\U00020FFF\U000E01EF-{0}", id);

                // Differs from nodeName1 only in characters above the Basic Multilingual plane.
                string nodeName3 = string.Format("$bvt_TestCreateUnicodePath-\u00AE\u017F\u01FF\u04FF\u26FF\uFB4F\U00010349\U00020EFF\U000E01EE-{0}", id);

                Assert.AreNotEqual(nodeName1, nodeName2);
                Assert.AreNotEqual(nodeName1, nodeName3);

                string nodePath1 = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName1);
                string nodePath2 = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName2);
                string nodePath3 = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName3);

                byte[] nodeData1 = Guid.NewGuid().ToByteArray();
                byte[] nodeData2 = Guid.NewGuid().ToByteArray();
                byte[] nodeData3 = Guid.NewGuid().ToByteArray();

                byte[] nodeAscii1 = Encoding.ASCII.GetBytes(nodeName1);
                byte[] nodeAscii2 = Encoding.ASCII.GetBytes(nodeName2);
                byte[] nodeAscii3 = Encoding.ASCII.GetBytes(nodeName3);

                VerifyBytesAreEqual(nodeAscii1, nodeAscii2);
                VerifyBytesAreEqual(nodeAscii1, nodeAscii3);

                string createdNodeName1 = await ringMaster.Create(nodePath1, nodeData1, null, CreateMode.Persistent);
                string createdNodeName2 = await ringMaster.Create(nodePath2, nodeData2, null, CreateMode.Persistent);
                string createdNodeName3 = await ringMaster.Create(nodePath3, nodeData3, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName1, nodeName1);
                Assert.AreEqual(createdNodeName2, nodeName2);
                Assert.AreEqual(createdNodeName3, nodeName3);

                IStat stat1 = await ringMaster.Exists(nodePath1, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat1, expectedDataLength: nodeData1.Length);

                IStat stat2 = await ringMaster.Exists(nodePath2, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat2, expectedDataLength: nodeData2.Length);

                IStat stat3 = await ringMaster.Exists(nodePath3, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat3, expectedDataLength: nodeData3.Length);

                byte[] retrievedNodeData1 = await ringMaster.GetData(nodePath1, watcher: null);
                byte[] retrievedNodeData2 = await ringMaster.GetData(nodePath2, watcher: null);
                byte[] retrievedNodeData3 = await ringMaster.GetData(nodePath3, watcher: null);

                VerifyBytesAreEqual(nodeData1, retrievedNodeData1);
                VerifyBytesAreEqual(nodeData2, retrievedNodeData2);
                VerifyBytesAreEqual(nodeData3, retrievedNodeData3);

                byte[] newNodeData1 = Guid.NewGuid().ToByteArray();
                byte[] newNodeData2 = Guid.NewGuid().ToByteArray();
                byte[] newNodeData3 = Guid.NewGuid().ToByteArray();

                stat1 = await ringMaster.SetData(nodePath1, newNodeData1, version: -1);
                stat2 = await ringMaster.SetData(nodePath2, newNodeData2, version: -1);
                stat3 = await ringMaster.SetData(nodePath3, newNodeData3, version: -1);

                retrievedNodeData1 = await ringMaster.GetData(nodePath1, watcher: null);
                retrievedNodeData2 = await ringMaster.GetData(nodePath2, watcher: null);
                retrievedNodeData3 = await ringMaster.GetData(nodePath3, watcher: null);

                VerifyBytesAreEqual(newNodeData1, retrievedNodeData1);
                VerifyBytesAreEqual(newNodeData2, retrievedNodeData2);
                VerifyBytesAreEqual(newNodeData3, retrievedNodeData3);

                var id1 = new Id(AuthSchemes.World, "Identifier-\u0100\u100F\uF90F\U00010480\U000E01EF");

                // Differs from id1 only non basic latin characters in the Basic Multilingual plane.
                var id2 = new Id(AuthSchemes.World, "Identifier-\u0100\u100E\uF90F\U00010480\U000E01EF");

                // Differs from id1 only in characters above the Basic Multilingual plane.
                var id3 = new Id(AuthSchemes.World, "Identifier-\u0100\u100F\uF90F\U00010480\U000E01EE");
                var acl1 = new Acl((int)Acl.Perm.ALL, id1);
                var acl2 = new Acl((int)Acl.Perm.ALL, id2);
                var acl3 = new Acl((int)Acl.Perm.ALL, id3);
                var aclList1 = new List<Acl>() { acl1, acl2, acl3 };
                var aclList2 = new List<Acl>() { acl2, acl3, acl1 };
                var aclList3 = new List<Acl>() { acl3, acl1, acl2 };

                stat1 = await ringMaster.SetACL(nodePath1, aclList1, -1);
                stat2 = await ringMaster.SetACL(nodePath2, aclList2, -1);
                stat3 = await ringMaster.SetACL(nodePath3, aclList3, -1);

                VerifyAclListsAreEqual(aclList1, await ringMaster.GetACL(nodePath1, stat1));
                VerifyAclListsAreEqual(aclList2, await ringMaster.GetACL(nodePath2, stat2));
                VerifyAclListsAreEqual(aclList3, await ringMaster.GetACL(nodePath3, stat3));

                await ringMaster.Sync(nodePath1);
                await ringMaster.Sync(nodePath2);
                await ringMaster.Sync(nodePath3);

                await ringMaster.Delete(nodePath1, -1);
                await ringMaster.Delete(nodePath2, -1);
                await ringMaster.Delete(nodePath3, -1);
            }
        }

        /// <summary>
        /// Verify that data sizes up to the limit are supported and the limit is enforced.
        /// </summary>
        /// <param name="maxDataLength">Maximum data limit</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateLargeData(int maxDataLength)
        {
            // RM enforces limits now which we test in the BackendCore unit tests
            // We don't test going over the limit here as the prototype implemention shares these tests and does not support limits
            // Nor do we need BVT to test that scenario (unit tests suffice)
            var random = new Random();

            string guid = Guid.NewGuid().ToString();
            using (var ringMaster = this.ConnectToRingMaster())
            {
                for (int dataLength = 1; dataLength < maxDataLength; dataLength *= 2)
                {
                    string nodeName = string.Format("$bvt_TestCreateLargeData-{0}-{1}", guid, dataLength);
                    string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
                    byte[] nodeData = new byte[dataLength];
                    random.NextBytes(nodeData);

                    await ringMaster.Create(nodePath, nodeData, null, CreateMode.Persistent);

                    VerifyBytesAreEqual(nodeData, await ringMaster.GetData(nodePath, watcher: null));

                    nodeData = new byte[dataLength + 1];
                    random.NextBytes(nodeData);

                    IStat newstat = await ringMaster.SetData(nodePath, nodeData, -1);

                    Assert.AreEqual(newstat.DataLength, nodeData.Length);

                    VerifyBytesAreEqual(nodeData, await ringMaster.GetData(nodePath, watcher: null));

                    await ringMaster.Delete(nodePath, -1);
                }
            }
        }

        /// <summary>
        /// Verify that a node can be deleted.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestDelete()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestDelete{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);

                string createdNodeName = await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat);

                bool result = await ringMaster.Delete(nodePath, stat.Version);
                Assert.IsTrue(result);

                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                // Verify that -1 can be specified as the version to override version check.
                result = await ringMaster.Delete(nodePath, -1);
                Assert.IsTrue(result);

                // Attempt to delete a non-existent node must return false.
                result = await ringMaster.Delete(nodePath, -1);
                Assert.IsFalse(result);
            }
        }

        /// <summary>
        /// Verify that a node can be fast-deleted.
        /// </summary>
        /// <param name="fastDelete">If <c>true</c> fast delete option will be used</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCascadeDelete(bool fastDelete)
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestCascadeDelete{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);

                await ringMaster.Sync("/");

                string createdNodeName = await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat);

                ulong numCreated = 1;
                ulong dataCreated = 0;

                byte[] data = new byte[] { 1, 2 };
                var childNodeNames = this.GeneratePaths(nodePath, 4, 10, false).ToArray();
                var ops = childNodeNames.Select(n => Op.Create(n, data, null, CreateMode.Persistent)).ToArray();
                var creationResult = await ringMaster.Multi(ops);
                Assert.AreEqual(ops.Length, creationResult.Count);
                for (int i = 0; i < ops.Length; i++)
                {
                    var createResult = (OpResult.CreateResult)creationResult[i];
                    Assert.IsTrue(childNodeNames[i].EndsWith(createResult.Path));
                    VerifyStatForFreshlyCreatedNode(createResult.Stat, data.Length);

                    numCreated++;
                    dataCreated += (ulong)data.Length;
                }

                foreach (var childNodeName in childNodeNames)
                {
                    Assert.IsNotNull(await ringMaster.Exists(childNodeName, watcher: null));
                }

                await ringMaster.Sync("/");

                ulong countIs = (ulong)await this.CountNodesFrom(ringMaster, nodePath);

                Assert.AreEqual(countIs, numCreated);

                stat = await ringMaster.Exists(nodePath, watcher: null);

                bool result = await ringMaster.Delete(nodePath, stat.Version, fastDelete ? (DeleteMode.CascadeDelete | DeleteMode.FastDelete) : DeleteMode.CascadeDelete);
                Assert.IsTrue(result);

                await ringMaster.Sync("/");

                await VerifyRingMasterException(RingMasterException.Code.Nonode, async () => await ringMaster.Exists(nodePath, watcher: null), $"{nodePath} should not exist");
            }
        }

        /// <summary>
        /// Verify that a node can be deleted.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestDeleteWithAllowNonExistFlag()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestDeleteWithAllowNonExistFlag{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);

                string createdNodeName = await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyStatForFreshlyCreatedNode(stat);

                bool result = await ringMaster.Delete(nodePath, stat.Version, DeleteMode.SuccessEvenIfNodeDoesntExist);
                Assert.IsTrue(result);

                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                // Verify that -1 can be specified as the version to override version check.
                result = await ringMaster.Delete(nodePath, -1, DeleteMode.SuccessEvenIfNodeDoesntExist);
                Assert.IsTrue(result);

                // Attempt to delete a non-existent node must return false.
                result = await ringMaster.Delete(nodePath, -1);
                Assert.IsFalse(result);

                // Attempt to delete a non-existent node with the deletemode set to SuccessEvenIfNodeDoesntExist must return true.
                result = await ringMaster.Delete(nodePath, -1, DeleteMode.SuccessEvenIfNodeDoesntExist);
                Assert.IsTrue(result, "delete with allowskip should work");
            }
        }

        /// <summary>
        /// Verify that data associated with a sub tree can be retrieved using the <c>path/$fullsubtree$</c>
        /// syntax.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetFullSubtreeData()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string rootNodeName = $"$bvt_TestGetFullSubtreeData{Guid.NewGuid()}";
                string rootPath = $"{TestFunctionality.TestPrefix}/{rootNodeName}";

                byte[] rootData = Guid.NewGuid().ToByteArray();

                string node1Path = rootPath + "/node1";
                string node2Path = rootPath + "/node2";

                byte[] node1Data = Guid.NewGuid().ToByteArray();
                byte[] node2Data = Guid.NewGuid().ToByteArray();

                await ringMaster.Create(rootPath, rootData, null, CreateMode.Persistent);
                await ringMaster.Create(node1Path, node1Data, null, CreateMode.Persistent);
                await ringMaster.Create(node2Path, node2Data, null, CreateMode.Persistent);

                Func<bool, Task> testFunc = async (withStat) =>
                {
                    TreeNode root = await ringMaster.GetFullSubtree(rootPath, withStat);

                    Assert.AreEqual(rootNodeName, root.Name);
                    VerifyBytesAreEqual(rootData, root.Data);
                    Assert.AreEqual(withStat, root.Stat != null);
                    Assert.AreEqual(2, root.Children.Count);

                    Assert.AreEqual("node1", root.Children[0].Name);
                    VerifyBytesAreEqual(node1Data, root.Children[0].Data);
                    Assert.AreEqual(withStat, root.Children[0].Stat != null);
                    Assert.IsNull(root.Children[0].Children);

                    Assert.AreEqual("node2", root.Children[1].Name);
                    VerifyBytesAreEqual(node2Data, root.Children[1].Data);
                    Assert.AreEqual(withStat, root.Children[1].Stat != null);
                    Assert.IsNull(root.Children[1].Children);
                };

                // Get full sub-tree with stat
                await testFunc(true);

                // Get full sub-tree without stat
                await testFunc(false);

                await ringMaster.Delete(rootPath, -1, DeleteMode.CascadeDelete);
            }
        }

        /// <summary>
        /// Verify that data to be associated with a node can be set after
        /// the node is created and that data can be retrieved using the GetData method.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestSetData()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestSetData{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);
                byte[] nodeData = Guid.NewGuid().ToByteArray();

                string createdNodeName = await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat statCreate = await ringMaster.Exists(nodePath, watcher: null);

                VerifyStatForFreshlyCreatedNode(statCreate, expectedDataLength: 0);

                IStat statSetData = await ringMaster.SetData(nodePath, nodeData, statCreate.Version);

                // Node has just been modified so Czxid (create transaction id)
                // must not be equal to Mzxid (modify transaction id).
                Assert.AreNotEqual(statSetData.Czxid, statSetData.Mzxid);

                // Czxid and Ctime must have the same values they had when the
                // node was created.
                Assert.AreEqual(statCreate.Czxid, statSetData.Czxid);

                // TODO (RDBug 6569426): Re-enable once this functionality is implemented.
                //// Assert.AreEqual(statCreate.Ctime, statSetData.Ctime);

                // Since no children were added or deleted, Pzxid must be
                // the same as Czxid.
                Assert.AreEqual(statSetData.Czxid, statSetData.Pzxid);

                // Data was set, so version must be 2
                Assert.AreEqual(2, statSetData.Version);

                // But no children were added or ACLs changed, so
                // those versions must be 1.
                Assert.AreEqual(1, statSetData.Cversion);
                Assert.AreEqual(1, statSetData.Aversion);

                // There are no children.
                Assert.AreEqual(0, statSetData.NumChildren);

                // The node has data.
                Assert.AreEqual(nodeData.Length, statSetData.DataLength);

                byte[] retrievedNodeData = await ringMaster.GetData(nodePath, watcher: null);

                VerifyBytesAreEqual(nodeData, retrievedNodeData);

                // Verify that -1 can be specified as the version to override version check
                statSetData = await ringMaster.SetData(nodePath, null, -1);

                // Data was set, so version must be 3
                Assert.AreEqual(3, statSetData.Version);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that Access control list to be associated with nodes can be provided at
        /// creation time and that data can be retrieved using the GetACL method.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateNodeWithAcl()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestCreateNodeWithAcl{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);

                var id1 = new Id(AuthSchemes.World, "Identifier1");
                var id2 = new Id(AuthSchemes.World, "Identifier2");
                var acl1 = new Acl((int)Acl.Perm.ALL, id1);
                var acl2 = new Acl((int)Acl.Perm.ALL, id2);
                var aclList = new List<Acl>() { acl1, acl2 };

                string createdNodeName = await ringMaster.Create(nodePath, null, aclList, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat stat = await ringMaster.Exists(nodePath, watcher: null);

                VerifyStatForFreshlyCreatedNode(stat, expectedDataLength: 0);

                IReadOnlyList<Acl> retrievedAcls = await ringMaster.GetACL(nodePath, stat);

                VerifyAclListsAreEqual(aclList, retrievedAcls);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that access control list to be associated with a node can be set after
        /// the node is created and that access control list can be retrieved using the
        /// GetACL method.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestSetAcl()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestSetAcl{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, nodeName);

                var id1 = new Id(AuthSchemes.World, "Identifier1");
                var id2 = new Id(AuthSchemes.World, "Identifier2");
                var acl1 = new Acl((int)Acl.Perm.ALL, id1);
                var acl2 = new Acl((int)Acl.Perm.ALL, id2);
                var aclList = new List<Acl>() { acl1, acl2 };

                string createdNodeName = await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                Assert.AreEqual(createdNodeName, nodeName);

                IStat statCreate = await ringMaster.Exists(nodePath, watcher: null);

                VerifyStatForFreshlyCreatedNode(statCreate, expectedDataLength: 0);

                IStat statSetAcl = await ringMaster.SetACL(nodePath, aclList, statCreate.Version);

                // Node has just been modified so Czxid (create transaction id)
                // must not be equal to Mzxid (modify transaction id).
                Assert.AreNotEqual(statSetAcl.Czxid, statSetAcl.Mzxid);

                // Czxid and Ctime must have the same values they had when the
                // node was created.
                Assert.AreEqual(statCreate.Czxid, statSetAcl.Czxid);

                // TODO (RDBug 6569426): Re-enable once this functionality is implemented.
                //// Assert.AreEqual(statCreate.Ctime, statSetAcl.Ctime);

                // Since no children were added or deleted, Pzxid must be
                // the same as Czxid.
                Assert.AreEqual(statSetAcl.Czxid, statSetAcl.Pzxid);

                // ACL was set, so Aversion must be 2
                Assert.AreEqual(2, statSetAcl.Aversion);

                // But no children were added or data changed, so
                // those versions must be 1.
                Assert.AreEqual(1, statSetAcl.Cversion);
                Assert.AreEqual(1, statSetAcl.Version);

                // There are no children and the node has no data.
                Assert.AreEqual(0, statSetAcl.NumChildren);
                Assert.AreEqual(0, statSetAcl.DataLength);

                IReadOnlyList<Acl> retrievedAcls = await ringMaster.GetACL(nodePath, statSetAcl);

                VerifyAclListsAreEqual(aclList, retrievedAcls);

                // Verify that -1 can be specified as the version to override version check
                statSetAcl = await ringMaster.SetACL(nodePath, new List<Acl>(), -1);

                // Data was set, so version must be 3
                Assert.AreEqual(3, statSetAcl.Aversion);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verifies that Multi operation works correctly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestMulti()
        {
            const int NumberOfNodes = 4;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                // Create a set of nodes using multi
                var createOperations = new List<Op>();
                var nodePathList = new List<string>();
                var nodeDataList = new List<byte[]>();
                for (int i = 0; i < NumberOfNodes; i++)
                {
                    var nodePath = string.Format("{0}/TestMulti_{1}", TestFunctionality.TestPrefix, Guid.NewGuid());
                    var nodeData = Guid.NewGuid().ToByteArray();

                    createOperations.Add(Op.Create(nodePath, nodeData, null, CreateMode.Persistent));
                    nodePathList.Add(nodePath);
                    nodeDataList.Add(nodeData);
                }

                // Verify that all creates succeeded.
                IReadOnlyList<OpResult> createResults = await ringMaster.Multi(createOperations);
                Assert.AreEqual(NumberOfNodes, createResults.Count);
                foreach (var result in createResults)
                {
                    Assert.AreEqual(OpCode.Create, result.ResultType);
                    Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);
                    Assert.IsNotNull(result is OpResult.CreateResult);
                }

                // Now retrieve the data for the created nodes using a multi operation
                var getDataOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    getDataOperations.Add(Op.GetData(nodePath, RequestGetData.GetDataOptions.None, null));
                }

                IReadOnlyList<OpResult> getDataResults = await ringMaster.Multi(getDataOperations);

                // Verify that all GetData operations succeeded and the retrieved data
                // is as expected
                Assert.AreEqual(NumberOfNodes, getDataResults.Count);
                for (int i = 0; i < NumberOfNodes; i++)
                {
                    var result = getDataResults[i];
                    var expectedData = nodeDataList[i];

                    Assert.AreEqual(OpCode.GetData, result.ResultType);
                    Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);

                    var getDataResult = result as OpResult.GetDataResult;
                    Assert.IsNotNull(getDataResult);
                    Assert.AreEqual(expectedData.Length, getDataResult.Bytes.Length);
                    Assert.IsTrue(expectedData.SequenceEqual(getDataResult.Bytes));
                }

                // Now change the data for all the nodes using a multi operation
                var setDataOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    setDataOperations.Add(Op.SetData(nodePath, Guid.NewGuid().ToByteArray(), 1));
                }

                IReadOnlyList<OpResult> setDataResults = await ringMaster.Multi(setDataOperations);

                // Verify that all SetData operations succeeded
                Assert.AreEqual(NumberOfNodes, setDataResults.Count);
                foreach (var result in setDataResults)
                {
                    Assert.AreEqual(OpCode.SetData, result.ResultType);
                    Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);
                    Assert.IsNotNull(result is OpResult.SetDataResult);
                }

                // Now check all nodes
                var checkOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    checkOperations.Add(Op.Check(nodePath, -1));
                }

                IReadOnlyList<OpResult> checkResults = await ringMaster.Multi(checkOperations);

                // Verify that all Check operations succeeded
                Assert.AreEqual(NumberOfNodes, checkResults.Count);
                foreach (var result in checkResults)
                {
                    Assert.AreEqual(OpCode.Check, result.ResultType);
                    Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);
                    Assert.IsNotNull(result is OpResult.CheckResult);
                }

                var runOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    var opCheck = Op.Check(nodePath, -1);
                    runOperations.Add(Op.Run(opCheck.Request));
                }

                IReadOnlyList<OpResult> runResults = await ringMaster.Multi(runOperations);

                // Verify that all Run operations succeeded
                Assert.AreEqual(NumberOfNodes, runResults.Count);
                foreach (var result in runResults)
                {
                    Assert.AreEqual(OpCode.Check, result.ResultType);
                    Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);
                    Assert.IsNotNull(result is OpResult.CheckResult);
                }

                // Now delete all nodes using a multi operation
                var deleteOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    deleteOperations.Add(Op.Delete(nodePath, 2));
                }

                IReadOnlyList<OpResult> deleteResults = await ringMaster.Multi(deleteOperations);

                // Verify that all Delete operations succeeded
                Assert.AreEqual(NumberOfNodes, deleteResults.Count);
                foreach (var result in deleteResults)
                {
                    Assert.AreEqual(OpCode.Delete, result.ResultType);
                    Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);
                    Assert.IsNotNull(result is OpResult.DeleteResult);
                }
            }
        }

        /// <summary>
        /// Verifies that Multi operation works correctly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestMultiScheduled()
        {
            const int NumberOfNodes = 4;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                Stopwatch sw = Stopwatch.StartNew();

                while (await ringMaster.Exists("/$metadata/scheduler/commands", null, true) == null)
                {
                    if (sw.ElapsedMilliseconds > 15000)
                    {
                        Console.WriteLine("There is no commands tree on the target cluster");
                        return;
                    }
                }

                // Create a set of nodes using multi
                var createOperations = new List<Op>();
                var nodePathList = new List<string>();
                var nodeDataList = new List<byte[]>();
                for (int i = 0; i < NumberOfNodes; i++)
                {
                    var nodePath = string.Format("{0}/TestMulti_{1}", TestFunctionality.TestPrefix, Guid.NewGuid());
                    var nodeData = Guid.NewGuid().ToByteArray();

                    createOperations.Add(Op.Create(nodePath, nodeData, null, CreateMode.Persistent));
                    nodePathList.Add(nodePath);
                    nodeDataList.Add(nodeData);
                }

                // Verify that all creates succeeded.
                bool createResults = await TestFunctionality.RunMultiAndWaitForCompletion(ringMaster, createOperations, Guid.NewGuid().ToString());
                Assert.IsTrue(createResults);

                foreach (var nodePath in nodePathList)
                {
                    IStat st = await ringMaster.Exists(nodePath, null, true);
                    Assert.IsNotNull(st);
                }

                // Now retrieve the data for the created nodes using a multi operation
                var getDataOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    getDataOperations.Add(Op.GetData(nodePath, RequestGetData.GetDataOptions.None, null));
                }

                bool getDataResults = await TestFunctionality.RunMultiAndWaitForCompletion(ringMaster, getDataOperations, Guid.NewGuid().ToString());

                // Verify that all GetData operations succeeded
                Assert.IsTrue(getDataResults);

                // Now change the data for all the nodes using a multi operation
                var setDataOperations = new List<Op>();
                Dictionary<string, Guid> newBytes = new Dictionary<string, Guid>();

                foreach (var nodePath in nodePathList)
                {
                    newBytes[nodePath] = Guid.NewGuid();
                    setDataOperations.Add(Op.SetData(nodePath, newBytes[nodePath].ToByteArray(), 1));
                }

                bool setDataResults = await TestFunctionality.RunMultiAndWaitForCompletion(ringMaster, setDataOperations, Guid.NewGuid().ToString());

                // Verify that all SetData operations succeeded
                Assert.IsTrue(setDataResults);

                foreach (var nodePath in nodePathList)
                {
                    byte[] changedData = await ringMaster.GetData(nodePath, null);
                    Guid newGuid = new Guid(changedData);
                    Assert.AreEqual(newGuid, newBytes[nodePath]);
                }

                // Now check all nodes
                var checkOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    checkOperations.Add(Op.Check(nodePath, -1));
                }

                bool checkResults = await TestFunctionality.RunMultiAndWaitForCompletion(ringMaster, checkOperations, Guid.NewGuid().ToString());

                // Verify that all Check operations succeeded
                Assert.IsTrue(checkResults);

                var runOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    var opCheck = Op.Check(nodePath, -1);
                    runOperations.Add(Op.Run(opCheck.Request));
                }

                bool runResults = await TestFunctionality.RunMultiAndWaitForCompletion(ringMaster, runOperations, Guid.NewGuid().ToString());

                // Verify that all Run operations succeeded
                Assert.IsTrue(runResults);

                // Now delete all nodes using a multi operation
                var deleteOperations = new List<Op>();
                foreach (var nodePath in nodePathList)
                {
                    deleteOperations.Add(Op.Delete(nodePath, 2));
                }

                bool deleteResults = await TestFunctionality.RunMultiAndWaitForCompletion(ringMaster, deleteOperations, Guid.NewGuid().ToString());

                // Verify that all Delete operations succeeded
                Assert.IsTrue(deleteResults);
                foreach (var nodePath in nodePathList)
                {
                    IStat st = await ringMaster.Exists(nodePath, null, true);
                    Assert.IsNull(st);
                }

                string badCommand = Guid.NewGuid().ToString();
                bool badDeleteResults = await TestFunctionality.RunMultiAndWaitForCompletion(ringMaster, deleteOperations, badCommand);

                // Verify that BAD Delete operations fail
                Assert.IsFalse(badDeleteResults);

                await TestFunctionality.DeleteFailedCommand(ringMaster, badCommand);
            }
        }

        /// <summary>
        /// Verifies that GetData operation on a node that has no data with <c>FaultbackOnParentData</c> option
        /// retrieves no data.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetDataFaultbackOnParentData()
        {
            await this.VerifyGetDataFaultbackOnParentData(RequestGetData.GetDataOptions.FaultbackOnParentData, null);
        }

        /// <summary>
        /// Verifies that GetData operation on a node that has no data with <c>FaultbackOnParentDataWithMatch</c> option
        /// retrieves parent node's data that matches the given argument.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetDataFaultbackOnParentDataWithMatch()
        {
            await this.VerifyGetDataFaultbackOnParentData(
                RequestGetData.GetDataOptions.FaultbackOnParentDataWithMatch,
                new RequestGetData.GetDataOptionArgumentForMatch(null, 0, RequestGetData.GetDataOptionArgumentForMatch.Comparison.Different));
        }

        /// <summary>
        /// Verifies that GetData operation on a non-existent node with <c>FaultbackOnParentData</c> option
        /// returns Nonode error.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetDataFaultbackOnAncestorData()
        {
            await this.VerifyGetDataFaultbackOnAncestorData(RequestGetData.GetDataOptions.FaultbackOnParentData, null);
        }

        /// <summary>
        /// Verifies that GetData operation on a non-existent node with <c>FaultbackOnParentDataWithMatch</c> option
        /// retrieves the last existing ancestor node's data that matches the given argument.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetDataFaultbackOnAncestorDataWithMatch()
        {
            await this.VerifyGetDataFaultbackOnAncestorData(
                RequestGetData.GetDataOptions.FaultbackOnParentDataWithMatch,
                new RequestGetData.GetDataOptionArgumentForMatch(null, 0, RequestGetData.GetDataOptionArgumentForMatch.Comparison.Different));
        }

        /// <summary>
        /// Verifies GetSubtree operation returns nodes in depth-first order and that continuations work as expected.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetSubtree(bool includeStat)
        {
            var parentName = $"$bvt_TestGetSubtree_{Guid.NewGuid()}";
            var parentPath = $"{TestFunctionality.TestPrefix}/{parentName}";
            var requestOptions = includeStat ? RequestGetSubtree.GetSubtreeOptions.IncludeStats : RequestGetSubtree.GetSubtreeOptions.None;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                await ringMaster.Create(parentPath, Encoding.UTF8.GetBytes("p"), null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b1", Encoding.UTF8.GetBytes("b1"), null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b1/c1", null, null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b1/c2", Encoding.UTF8.GetBytes("b1c2"), null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b2", Encoding.UTF8.GetBytes("b2"), null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b4", null, null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b4/c1", Encoding.UTF8.GetBytes("b4c1"), null, CreateMode.Persistent);

                // for the first response, we expect /p, /p/b1, and /p/b1/c1
                var response = await ringMaster.GetSubtree(parentPath, ">:3:", requestOptions);
                var treeRoot = response.Subtree;
                response.ContinuationPath.Should().Be($"{parentPath}/b1/c1");

                // /p
                treeRoot.Name.Should().Be(parentName);
                Encoding.UTF8.GetString(treeRoot.Data).Should().Be("p");
                Assert.AreEqual(includeStat, treeRoot.Stat != null);
                treeRoot.Children.Count.Should().Be(1);

                // /p/b1
                treeRoot.Children[0].Name.Should().Be("b1");
                Encoding.UTF8.GetString(treeRoot.Children[0].Data).Should().Be("b1");
                Assert.AreEqual(includeStat, treeRoot.Children[0].Stat != null);
                treeRoot.Children[0].Children.Count.Should().Be(1);

                // /p/b1/c1
                treeRoot.Children[0].Children[0].Name.Should().Be("c1");
                treeRoot.Children[0].Children[0].Data.Should().Be(null);
                Assert.AreEqual(includeStat, treeRoot.Children[0].Children[0].Stat != null);
                treeRoot.Children[0].Children[0].Children.Should().BeNullOrEmpty();

                // second response we expect /p, /p/b1, /p/b1/c1 (from continuation, without data/stats), plus /p/b1/c2, /p/b2, /p/b4
                response = await ringMaster.GetSubtree(parentPath, $">:3:{response.ContinuationPath}", requestOptions);
                treeRoot = response.Subtree;
                response.ContinuationPath.Should().Be($"{parentPath}/b4");

                // /p
                treeRoot.Name.Should().Be(parentName);
                treeRoot.Data.Should().Be(null);
                treeRoot.Stat.Should().Be(null);
                treeRoot.Children.Count.Should().Be(3);

                // /p/b1
                treeRoot.Children[0].Name.Should().Be("b1");
                treeRoot.Children[0].Data.Should().Be(null);
                treeRoot.Children[0].Stat.Should().Be(null);
                treeRoot.Children[0].Children.Count.Should().Be(2);

                // /p/b1/c1
                treeRoot.Children[0].Children[0].Name.Should().Be("c1");
                treeRoot.Children[0].Children[0].Data.Should().Be(null);
                treeRoot.Children[0].Children[0].Stat.Should().Be(null);
                treeRoot.Children[0].Children[0].Children.Should().BeNullOrEmpty();

                // /p/b1/c2
                treeRoot.Children[0].Children[1].Name.Should().Be("c2");
                Encoding.UTF8.GetString(treeRoot.Children[0].Children[1].Data).Should().Be("b1c2");
                Assert.AreEqual(includeStat, treeRoot.Children[0].Children[1].Stat != null);
                treeRoot.Children[0].Children[1].Children.Should().BeNullOrEmpty();

                // /p/b2
                treeRoot.Children[1].Name.Should().Be("b2");
                Encoding.UTF8.GetString(treeRoot.Children[1].Data).Should().Be("b2");
                Assert.AreEqual(includeStat, treeRoot.Children[1].Stat != null);
                treeRoot.Children[1].Children.Should().BeNullOrEmpty();

                // /p/b4
                treeRoot.Children[2].Name.Should().Be("b4");
                treeRoot.Children[2].Data.Should().Be(null);
                Assert.AreEqual(includeStat, treeRoot.Children[2].Stat != null);
                treeRoot.Children[2].Children.Should().BeNullOrEmpty();

                // third response we expect /p and /p/b4 (from continuation, without data), plus /p/b4/c1
                // and the continuation should now be null since we are at the end
                response = await ringMaster.GetSubtree(parentPath, $">:3:{response.ContinuationPath}", requestOptions);
                treeRoot = response.Subtree;
                response.ContinuationPath.Should().Be(null);

                // /p
                treeRoot.Name.Should().Be(parentName);
                treeRoot.Data.Should().Be(null);
                treeRoot.Stat.Should().Be(null);
                treeRoot.Children.Count.Should().Be(1);

                // /p/b4
                treeRoot.Children[0].Name.Should().Be("b4");
                treeRoot.Children[0].Data.Should().Be(null);
                treeRoot.Children[0].Stat.Should().Be(null);
                treeRoot.Children[0].Children.Count.Should().Be(1);

                // /p/b4/c1
                treeRoot.Children[0].Children[0].Name.Should().Be("c1");
                Encoding.UTF8.GetString(treeRoot.Children[0].Children[0].Data).Should().Be("b4c1");
                Assert.AreEqual(includeStat, treeRoot.Children[0].Children[0].Stat != null);
                treeRoot.Children[0].Children[0].Children.Should().BeNullOrEmpty();
            }
        }

        /// <summary>
        /// Verifies that GetSubtree request returns proper results if continuation path does not exist (e.g. has been deleted)
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetSubtreeContinuationContainsNonExistentPath()
        {
            string parentName = $"$bvt_GetSubtreeContinuationContainsNonExistentPath_{Guid.NewGuid()}";
            string parentPath = $"{TestFunctionality.TestPrefix}/{parentName}";

            using (var ringMaster = this.ConnectToRingMaster())
            {
                await ringMaster.Create(parentPath, Encoding.UTF8.GetBytes("p"), null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b1", null, null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b1/c1", null, null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b2", null, null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b4", Encoding.UTF8.GetBytes("b4"), null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b4/c1", Encoding.UTF8.GetBytes("b4c1"), null, CreateMode.Persistent);

                var response = await ringMaster.GetSubtree(parentPath, $">:1:{parentPath}/b3/c1");
                var treeRoot = response.Subtree;
                response.ContinuationPath.Should().Be($"{parentPath}/b4");

                treeRoot.Name.Should().Be(parentName);
                treeRoot.Data.Should().Be(null);
                treeRoot.Children.Count.Should().Be(1);

                treeRoot.Children[0].Name.Should().Be("b4");
                Encoding.UTF8.GetString(treeRoot.Children[0].Data).Should().Be("b4");
                treeRoot.Children[0].Children.Should().BeNullOrEmpty();
            }
        }

        /// <summary>
        /// Verifies that GetSubtree request case of asking for single node where continuation path will match the node path.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetSubtreeContinuationPathEqualsNodePath()
        {
            string parentName = $"$bvt_GetSubtreeContinuationPathEqualsNodePath_{Guid.NewGuid()}";
            string parentPath = $"{TestFunctionality.TestPrefix}/{parentName}";

            using (var ringMaster = this.ConnectToRingMaster())
            {
                await ringMaster.Create(parentPath, Encoding.UTF8.GetBytes("p"), null, CreateMode.Persistent);
                await ringMaster.Create($"{parentPath}/b1", Encoding.UTF8.GetBytes("b1"), null, CreateMode.Persistent);

                var response = await ringMaster.GetSubtree(parentPath, $">:1:");
                var treeRoot = response.Subtree;
                response.ContinuationPath.Should().Be($"{parentPath}");

                treeRoot.Name.Should().Be(parentName);
                Encoding.UTF8.GetString(treeRoot.Data).Should().Be("p");
                treeRoot.Children.Should().BeNullOrEmpty();

                response = await ringMaster.GetSubtree(parentPath, $">:1:{response.ContinuationPath}");
                treeRoot = response.Subtree;

                treeRoot.Name.Should().Be(parentName);
                treeRoot.Data.Should().Be(null);
                treeRoot.Children.Count.Should().Be(1);

                treeRoot.Children[0].Name.Should().Be("b1");
                Encoding.UTF8.GetString(treeRoot.Children[0].Data).Should().Be("b1");
                treeRoot.Children[0].Children.Should().BeNullOrEmpty();
            }
        }

        /// <summary>
        /// Verifies that GetSubtree request on the root node handles continuations properly.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestGetSubtreeOfRoot()
        {
            string nodeName = $"$bvt_GetSubtreeContinuationPathEqualsNodePath_{Guid.NewGuid()}";
            string nodePath = $"{TestFunctionality.TestPrefix}/{nodeName}";

            using (var ringMaster = this.ConnectToRingMaster())
            {
                // ensure we have at least one node under the root
                await ringMaster.Create(nodePath, Encoding.UTF8.GetBytes("p"), null, CreateMode.Persistent);

                var response = await ringMaster.GetSubtree("/", ">:1:");
                var treeRoot = response.Subtree;
                response.ContinuationPath.Should().Be("/");

                treeRoot.Name.Should().Be("/");
                treeRoot.Children.Should().BeNullOrEmpty();

                response = await ringMaster.GetSubtree("/", $">:1:{response.ContinuationPath}");
                treeRoot = response.Subtree;

                treeRoot.Name.Should().Be("/");
                treeRoot.Children.Count.Should().Be(1);
            }
        }

        /// <summary>
        /// Verifies that Exists, Sync, and GetSubtree work correctly in batch requests.
        /// </summary>
        public async Task TestExistsSyncGetSubtreeBatch()
        {
            string nodeName = $"$bvt_ExistsSyncGetSubtreeBatch_{Guid.NewGuid()}";
            string nodePath = $"{TestFunctionality.TestPrefix}/{nodeName}";

            using (var ringMaster = this.ConnectToRingMaster())
            {
                // ensure we have at least one node under the root
                await ringMaster.Create(nodePath, Encoding.UTF8.GetBytes("p"), null, CreateMode.Persistent);
                await ringMaster.Create($"{nodePath}/c1", Encoding.UTF8.GetBytes("c1data"), null, CreateMode.Persistent);

                var operations = new List<Op>();
                operations.Add(Op.Exists(nodePath));
                operations.Add(Op.Sync(nodePath));
                operations.Add(Op.GetSubtree(nodePath, ">:1:", RequestGetSubtree.GetSubtreeOptions.None));
                operations.Add(Op.GetSubtree(nodePath, $">:5:{nodePath}", RequestGetSubtree.GetSubtreeOptions.IncludeStats));

                var batchResults = await ringMaster.Batch(operations, true);
                batchResults.Count.Should().Be(4);

                batchResults[0].ErrCode.Should().Be(RingMasterException.Code.Ok);
                batchResults[0].ResultType.Should().Be(OpCode.Exists);

                var existsStat = ((OpResult.ExistsResult)batchResults[0]).Stat;
                Assert.IsNotNull(existsStat);

                batchResults[1].ErrCode.Should().Be(RingMasterException.Code.Ok);
                batchResults[1].ResultType.Should().Be(OpCode.Sync);
                batchResults[1].Should().BeOfType<OpResult.SyncResult>();

                batchResults[2].ErrCode.Should().Be(RingMasterException.Code.Ok);
                batchResults[2].ResultType.Should().Be(OpCode.GetSubtree);

                var getSubtreeResult = (OpResult.GetSubtreeResult)batchResults[2];
                var treeNode = TreeNode.Deserialize(getSubtreeResult.SerializedSubtree);
                treeNode.Name.Should().Be(nodeName);
                Encoding.UTF8.GetString(treeNode.Data).Should().Be("p");
                treeNode.Stat.Should().Be(null);
                treeNode.Children.Should().BeNullOrEmpty();
                getSubtreeResult.ContinuationPath.Should().Be(nodePath);

                batchResults[3].ErrCode.Should().Be(RingMasterException.Code.Ok);
                batchResults[3].ResultType.Should().Be(OpCode.GetSubtree);

                getSubtreeResult = (OpResult.GetSubtreeResult)batchResults[3];
                treeNode = TreeNode.Deserialize(getSubtreeResult.SerializedSubtree);
                treeNode.Name.Should().Be(nodeName);
                treeNode.Data.Should().Be(null);
                treeNode.Stat.Should().Be(null);
                treeNode.Children.Count.Should().Be(1);
                treeNode.Children[0].Name.Should().Be("c1");
                Encoding.UTF8.GetString(treeNode.Children[0].Data).Should().Be("c1data");
                Assert.IsNotNull(treeNode.Children[0].Stat);
                getSubtreeResult.ContinuationPath.Should().Be(null);
            }
        }

        /// <summary>
        /// Verifies that lock collision in Multi operation is handled properly
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task GetLockCollisionInMulti()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                // At the 3rd level, there are 2500 lock objects by default (see GetLockSizesPerLevelFromConfig in
                // Node.cs). If node count at this level is greater than lock collision will happen.
                const int nodeCount = 3000;
                var data0 = BitConverter.GetBytes(0);
                var data1 = BitConverter.GetBytes(1);

                var operations = new List<Op>();
                for (int i = 0; i < nodeCount; i++)
                {
                    operations.Add(Op.Create($"/$rmbvt/N{i:0000}", data0, null, CreateMode.PersistentAllowPathCreation));
                }

                await ringMaster.Multi(operations, true);

                operations.Clear();

                for (int i = 0; i < 10; i++)
                {
                    operations.Add(Op.GetData($"/$rmbvt/N{i:0000}", RequestGetData.GetDataOptions.None, null));
                }

                for (int i = 10; i < nodeCount; i++)
                {
                    operations.Add(Op.SetData($"/$rmbvt/N{i:0000}", data1, -1));
                }

                await ringMaster.Multi(operations, true);
            }
        }

        /// <summary>
        /// Runs the multi in the background and wait for completion.
        /// </summary>
        /// <param name="ringMaster">The ring master.</param>
        /// <param name="operations">The operations.</param>
        /// <param name="scheduleName">Name of the schedule.</param>
        /// <returns>a boolean indicating the success of the background completion</returns>
        private static async Task<bool> RunMultiAndWaitForCompletion(IRingMasterRequestHandler ringMaster, List<Op> operations, string scheduleName)
        {
            await ringMaster.Multi(operations, scheduleName, true);

            while (true)
            {
                bool isInCommands = await ringMaster.Exists("/$metadata/scheduler/commands/" + scheduleName, null, true) != null;

                bool isInflight = await ringMaster.Exists("/$metadata/scheduler/inflight/" + scheduleName, null, true) != null;

                if (!isInCommands && !isInflight)
                {
                    break;
                }

                await Task.Delay(500);
            }

            if (await ringMaster.Exists("/$metadata/scheduler/failures/" + scheduleName, null, true) != null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Deletes the failed command from the tree.
        /// </summary>
        /// <param name="ringMaster">The ring master.</param>
        /// <param name="scheduleName">Name of the schedule.</param>
        /// <returns>the task upon completion the command will be deleted</returns>
        private static async Task DeleteFailedCommand(IRingMasterRequestHandler ringMaster, string scheduleName)
        {
            if (await ringMaster.Exists("/$metadata/scheduler", null, true) == null)
            {
                return;
            }

            await ringMaster.Delete("/$metadata/scheduler/failures/" + scheduleName, -1, true);
        }

        /// <summary>
        /// Verify that all fields of the <see cref="IStat"/> data structure are set
        /// to sensible values for a newly created node.
        /// </summary>
        /// <param name="stat">The stat to verify</param>
        /// <param name="expectedDataLength">Expected data length for the newly created node</param>
        /// <param name="context">The context in which this verification is being performed</param>
        private static void VerifyStatForFreshlyCreatedNode(IStat stat, int expectedDataLength = 0, string context = null)
        {
            // Node exists, so stat should not be null
            Assert.IsNotNull(stat);

            // Node has just been created so Czxid (create transaction id)
            // must be equal to Mzxid (modify transaction id). Similarly,
            // Ctime must be equal to Mtime.
            Assert.AreEqual(stat.Czxid, stat.Mzxid, string.Format("Czxid vs Mzxid {0}", context));
            Assert.AreEqual(stat.Ctime, stat.Mtime, string.Format("Ctime vs Mtime {0}", context));

            // Since no children were added or deleted, Pzxid must be
            // the same as Czxid.
            Assert.AreEqual(stat.Czxid, stat.Pzxid);

            // No Changes yet, so version must be 1.
            Assert.AreEqual(1, stat.Version);
            Assert.AreEqual(1, stat.Cversion);
            Assert.AreEqual(1, stat.Aversion);

            // There are no children and the node has no data.
            Assert.AreEqual(0, stat.NumChildren);
            Assert.AreEqual(expectedDataLength, stat.DataLength);
        }

        private async Task VerifyCreateEphemeralNode(string nodeName, string nodePath, CreateMode createMode)
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string createdNodeName = await ringMaster.Create(nodePath, null, null, createMode);

                Assert.AreEqual(createdNodeName, nodeName);

                // Ephemeral node created by a session must exist as long as the session
                // is still active.
                IStat stat = await ringMaster.Exists(nodePath, watcher: null);

                VerifyStatForFreshlyCreatedNode(stat);
            }

            // Ephemeral node created by one session should not exist after
            // that session has been disconnected.
            using (var ringMaster2 = this.ConnectToRingMaster())
            {
                try
                {
                    // We give 15 seconds to cleanup the session
                    DateTime maxTime = DateTime.UtcNow + TimeSpan.FromSeconds(15);
                    while (DateTime.UtcNow < maxTime)
                    {
                        IStat statAfterSessionClose = await ringMaster2.Exists(nodePath, watcher: null);
                        System.Threading.Thread.Sleep(1000);
                    }

                    Assert.Fail("Node {0} exists", nodePath);
                }
                catch (RingMasterException exception)
                {
                    Assert.AreEqual(RingMasterException.Code.Nonode, exception.ErrorCode);
                }
            }
        }

        private async Task VerifyCreateEphemeralSequentialNode(string rootPath, string intermediatePath, CreateMode createMode)
        {
            string nodePath;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation($"Creating rootPath={rootPath}");
                await ringMaster.Create(rootPath, null, null, CreateMode.Persistent);

                string basePath = (intermediatePath != null) ? $"{rootPath}/{intermediatePath}" : rootPath;

                Trace.TraceInformation($"Creating nodePath={basePath}/node");
                string createdNodeName = await ringMaster.Create($"{basePath}/node", null, null, createMode);

                nodePath = basePath + "/" + createdNodeName;

                Assert.AreEqual("node0000000001", createdNodeName);

                // Ephemeral node created by a session must exist as long as the session
                // is still active.
                IStat stat = await ringMaster.Exists(nodePath, watcher: null);

                VerifyStatForFreshlyCreatedNode(stat);

                // Verify that nodes get increasing suffixes
                Assert.AreEqual("node0000000002", await ringMaster.Create(basePath + "/node", null, null, createMode));
                Assert.AreEqual("node0000000003", await ringMaster.Create(basePath + "/node", null, null, createMode));
                Assert.AreEqual("node0000000004", await ringMaster.Create(basePath + "/node", null, null, createMode));
            }

            // Ephemeral sequential node created by one session should not exist after
            // that session has been disconnected.
            using (var ringMaster2 = this.ConnectToRingMaster())
            {
                try
                {
                    // We give 15 seconds to cleanup the session
                    DateTime maxTime = DateTime.UtcNow + TimeSpan.FromSeconds(15);
                    while (DateTime.UtcNow < maxTime)
                    {
                        IStat statAfterSessionClose = await ringMaster2.Exists(nodePath, watcher: null);
                        System.Threading.Thread.Sleep(1000);
                    }

                    Assert.Fail("Node {0} exists", nodePath);
                }
                catch (RingMasterException exception)
                {
                    Assert.AreEqual(RingMasterException.Code.Nonode, exception.ErrorCode);
                }

                IStat rootStat = await ringMaster2.Exists(rootPath, watcher: null);
                Assert.IsTrue(await ringMaster2.Delete(rootPath, rootStat.Version, isRecursive: true));
            }
        }

        private async Task VerifyCreatePersistentNode(string nodeName, string nodePath, CreateMode createMode)
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string createdNodeName = await ringMaster.Create(nodePath, null, null, createMode);

                Assert.AreEqual(createdNodeName, nodeName);

                // Persistent node created by a session must exist while the session
                // is still active.
                IStat stat = await ringMaster.Exists(nodePath, watcher: null);

                VerifyStatForFreshlyCreatedNode(stat);
            }

            // Persistent node created by one session should exist after
            // that session has been disconnected.
            using (var ringMaster2 = this.ConnectToRingMaster())
            {
                IStat statAfterSessionClose = await ringMaster2.Exists(nodePath, watcher: null);

                Assert.AreEqual(0, statAfterSessionClose.NumChildren);
                Assert.AreEqual(0, statAfterSessionClose.DataLength);

                Assert.IsTrue(await ringMaster2.Delete(nodePath, statAfterSessionClose.Version));

                try
                {
                    IStat statAfterDelete = await ringMaster2.Exists(nodePath, watcher: null);
                    Assert.Fail("Node {0} exists", nodePath);
                }
                catch (RingMasterException exception)
                {
                    Assert.AreEqual(RingMasterException.Code.Nonode, exception.ErrorCode);
                }
            }
        }

        private async Task VerifyCreatePersistentSequentialNode(string rootPath, string intermediatePath, CreateMode createMode)
        {
            string node1Path;
            string node2Path;
            string node3Path;
            string node4Path;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                Trace.TraceInformation($"Creating rootPath {rootPath}");
                await ringMaster.Create(rootPath, null, null, CreateMode.Persistent);

                string basePath = (intermediatePath != null) ? $"{rootPath}/{intermediatePath}" : rootPath;
                string nodePath = $"{basePath}/node";

                string createdNodeName = await ringMaster.Create(nodePath, null, null, createMode);

                node1Path = basePath + "/" + createdNodeName;

                Assert.AreEqual("node0000000001", createdNodeName);

                IStat stat = await ringMaster.Exists(node1Path, watcher: null);

                VerifyStatForFreshlyCreatedNode(stat);

                // Verify that nodes get increasing suffixes
                string node2Name = await ringMaster.Create(nodePath, null, null, createMode);
                string node3Name = await ringMaster.Create(nodePath, null, null, createMode);
                string node4Name = await ringMaster.Create(nodePath, null, null, createMode);

                Assert.AreEqual("node0000000002", node2Name);
                Assert.AreEqual("node0000000003", node3Name);
                Assert.AreEqual("node0000000004", node4Name);

                node2Path = basePath + "/" + node2Name;
                node3Path = basePath + "/" + node3Name;
                node4Path = basePath + "/" + node4Name;
            }

            // PersistentSequential nodes created by one session should exist after
            // that session has been disconnected.
            using (var ringMaster2 = this.ConnectToRingMaster())
            {
                IStat statNode1 = await ringMaster2.Exists(node1Path, watcher: null);
                IStat statNode2 = await ringMaster2.Exists(node2Path, watcher: null);
                IStat statNode3 = await ringMaster2.Exists(node3Path, watcher: null);
                IStat statNode4 = await ringMaster2.Exists(node4Path, watcher: null);
                IStat statRoot = await ringMaster2.Exists(rootPath, watcher: null);

                Assert.IsTrue(await ringMaster2.Delete(node1Path, statNode1.Version));
                Assert.IsTrue(await ringMaster2.Delete(node2Path, statNode2.Version));
                Assert.IsTrue(await ringMaster2.Delete(node3Path, statNode3.Version));
                Assert.IsTrue(await ringMaster2.Delete(node4Path, statNode4.Version));
                Assert.IsTrue(await ringMaster2.Delete(rootPath, statRoot.Version, isRecursive: true));
            }
        }

        /// <summary>
        /// Verifies that GetData operation on a non-existent node with <c>FaultbackOnParentData</c> option
        /// retrieves parent node's data.
        /// </summary>
        /// <param name="options">Options for get data</param>
        /// <param name="optionArgument">Option argument for match</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        private async Task VerifyGetDataFaultbackOnParentData(RequestGetData.GetDataOptions options, RequestGetData.IGetDataOptionArgument optionArgument)
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string parentNodeName = string.Format("TestGetFaultBackOnParentData_Parent_{0}", Guid.NewGuid());
                string parentPath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, parentNodeName);
                var parentNodeData = Guid.NewGuid().ToByteArray();

                await ringMaster.Create(parentPath, parentNodeData, null, CreateMode.Persistent);

                // Issue a multi GetData request for a node with no data with
                // FaultbackOnParentData option
                string childPath = string.Format("{0}/child", parentPath);
                await ringMaster.Create(childPath, null, null, CreateMode.Persistent);

                var operations = new List<Op>();
                operations.Add(
                    Op.GetData(
                        childPath,
                        options,
                        optionArgument,
                        null));

                IReadOnlyList<OpResult> results = await ringMaster.Multi(operations);

                await ringMaster.Delete(parentPath, -1, DeleteMode.CascadeDelete);

                Assert.AreEqual(1, results.Count);

                // The operation should have succeeded and the parent node's
                // data must have been retrieved.
                var result = results[0] as OpResult.GetDataResult;
                Assert.IsNotNull(result);
                Assert.AreEqual(OpCode.GetData, result.ResultType);
                Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);

                if (optionArgument != null)
                {
                    Assert.IsNotNull(result.Bytes);
                    Assert.AreEqual(parentNodeData.Length, result.Bytes.Length);
                    CollectionAssert.AreEqual(parentNodeData, result.Bytes);
                }
                else
                {
                    Assert.IsNull(result.Bytes);
                }
            }
        }

        /// <summary>
        /// Verifies that GetData operation on a non-existent node with <c>FaultbackOnParentData</c> option
        /// retrieves the last existing ancestor node's data.
        /// </summary>
        /// <param name="options">Options for get data</param>
        /// <param name="optionArgument">Option argument for match</param>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        private async Task VerifyGetDataFaultbackOnAncestorData(
            RequestGetData.GetDataOptions options,
            RequestGetData.IGetDataOptionArgument optionArgument = null)
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string parentNodeName = string.Format("TestGetFaultBackOnAncestorData_Parent_{0}", Guid.NewGuid());
                string parentPath = string.Format("{0}/{1}", TestFunctionality.TestPrefix, parentNodeName);
                var parentNodeData = Guid.NewGuid().ToByteArray();

                await ringMaster.Create(parentPath, parentNodeData, null, CreateMode.Persistent);

                // Issue a multi GetData request for a non-existent node
                // FaultbackOnParentData option
                string childPath = string.Format("{0}/child/child2/child3", parentPath);

                var operations = new List<Op>();
                operations.Add(
                    Op.GetData(
                        childPath,
                        options,
                        optionArgument,
                        null));

                IReadOnlyList<OpResult> results = await ringMaster.Multi(operations);

                await ringMaster.Delete(parentPath, -1, DeleteMode.CascadeDelete);

                Assert.AreEqual(1, results.Count);

                if (optionArgument != null)
                {
                    // The operation should have succeeded and the ancestor node's
                    // data must have been retrieved.
                    var result = results[0] as OpResult.GetDataResult;
                    Assert.IsNotNull(result);
                    Assert.AreEqual(OpCode.GetData, result.ResultType);
                    Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);

                    Assert.IsNotNull(result.Bytes);
                    Assert.AreEqual(parentNodeData.Length, result.Bytes.Length);
                    CollectionAssert.AreEqual(parentNodeData, result.Bytes);
                }
                else
                {
                    // Nonode error should have been returned if there is on option argument
                    var result = results[0] as OpResult.ErrorResult;
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.ErrCode, RingMasterException.Code.Nonode);
                }
            }
        }

        /// <summary>
        /// Generates all paths to be created in depth first order.
        /// </summary>
        /// <param name="prefix">The path prefix.</param>
        /// <param name="numLevels">The number levels.</param>
        /// <param name="numNodesPerLevel">The number nodes per level.</param>
        /// <param name="onlyLeaves">if true, only the paths to the leaves are generated</param>
        /// <returns>Sequence of generated paths</returns>
        private IEnumerable<string> GeneratePaths(string prefix, int numLevels, int numNodesPerLevel, bool onlyLeaves)
        {
            if (numLevels <= 0)
            {
                throw new ArgumentException("numLevels must be >0");
            }

            if (numNodesPerLevel <= 0)
            {
                throw new ArgumentException("numNodesPerLevel must be >0");
            }

            for (int i = 0; i < numNodesPerLevel; i++)
            {
                string newPrefix = prefix + "/child" + i;

                if (numLevels == 1 || !onlyLeaves)
                {
                    yield return newPrefix;
                }

                if (numLevels > 1)
                {
                    foreach (string s in this.GeneratePaths(newPrefix, numLevels - 1, numNodesPerLevel, onlyLeaves))
                    {
                        yield return s;
                    }
                }
            }
        }

        private async Task<int> CountNodesFrom(IRingMasterRequestHandler ringMaster, string path)
        {
            try
            {
                var rootNode = await ringMaster.GetFullSubtree(path, true);
                return CountNode(rootNode);
            }
            catch (RingMasterException e)
            {
                if (e.ErrorCode != RingMasterException.Code.Nonode)
                {
                    return 0;
                }

                throw;
            }

            int CountNode(TreeNode node)
            {
                var count = 1;
                if (node.Children != null && node.Children.Count != 0)
                {
                    // Stat.NumChildren is intentionally left to be zero during serialization.
                    // Assert.AreEqual(node.Stat.NumChildren, node.Children.Count);
                    count += node.Children.Select(CountNode).Sum();
                }

                return count;
            }
        }

        private class AssertEx
        {
            public static void IsNotGreaterThan(long a, long b, string message = null)
            {
                if (a <= b)
                {
                    return;
                }

                if (message == null)
                {
                    message = string.Format("{0} should be <= {1}", a, b);
                }

                Assert.Fail("AssertEx.IsNotGreaterThan failed. " + message);
            }

            public static void IsGreaterThan(long a, long b, string message = null)
            {
                if (a > b)
                {
                    return;
                }

                if (message == null)
                {
                    message = string.Format("{0} should be > {1}", a, b);
                }

                Assert.Fail("AssertEx.IsGreaterThan failed. " + message);
            }
        }
    }
}
