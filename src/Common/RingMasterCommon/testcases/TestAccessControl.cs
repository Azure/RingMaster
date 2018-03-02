// <copyright file="TestAccessControl.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify that access control lists on nodes are handled correctly.
    /// </summary>
    public sealed class TestAccessControl : RingMasterInterfaceTest
    {
        /// <summary>
        /// The prefix which this test will use to create nodes
        /// </summary>
        private const string TestPrefix = "/$rmbvt/TestAccessControl";

        private const string AuthDigest1 = "b180ee5f-56ba-430e-a20d-ecaba4ab4fab";

        private const string AuthDigest2 = "48dd2f1d-f3ca-4d5e-903c-79f3f0b78bbe";

        private static readonly Id Digest1Identity = new Id(AuthSchemes.Digest, AuthDigest1);

        private static readonly Id Digest2Identity = new Id(AuthSchemes.Digest, AuthDigest2);

        private static readonly Acl AllowDigest1 = new Acl((int)Acl.Perm.ALL, Digest1Identity);

        private static readonly Acl AllowDigest2 = new Acl((int)Acl.Perm.ALL, Digest2Identity);

        /// <summary>
        /// Initializes the test.
        /// </summary>
        public void Initialize()
        {
            Task.Run(async () =>
            {
                using (var ringMaster = this.ConnectToRingMaster())
                {
                    Trace.TraceInformation("Initializing TestRoot {0}", TestAccessControl.TestPrefix);
                    try
                    {
                        await ringMaster.Create(TestAccessControl.TestPrefix, null, null, CreateMode.PersistentAllowPathCreation);
                    }
                    catch (RingMasterException ex)
                    {
                        Assert.AreEqual(RingMasterException.Code.Nodeexists, ex.ErrorCode);
                    }
                }
            }).Wait();
        }

        /// <summary>
        /// Verify that if a node has no access control settings, then clients with any identity can access it.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestNoAccessControl()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestNoAccessControl{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestAccessControl.TestPrefix, nodeName);
                byte[] nodeData = Guid.NewGuid().ToByteArray();
                byte[] replacementData = Guid.NewGuid().ToByteArray();

                await ringMaster.Create(nodePath, nodeData, null, CreateMode.Persistent);

                // Node should be accessible when the client has no identity
                IStat stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyBytesAreEqual(nodeData, await ringMaster.GetData(nodePath, watcher: null));
                Assert.AreEqual(null, await ringMaster.GetACL(nodePath, stat));
                await ringMaster.SetData(nodePath, nodeData, -1);

                // Node should be accessible when the client has Digest1 identity
                await ringMaster.SetAuth(Digest1Identity);

                stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyBytesAreEqual(nodeData, await ringMaster.GetData(nodePath, watcher: null));
                Assert.AreEqual(null, await ringMaster.GetACL(nodePath, stat));
                await ringMaster.SetData(nodePath, nodeData, -1);

                // Node should be accessible when the client has Digest2 identity
                await ringMaster.SetAuth(Digest2Identity);

                stat = await ringMaster.Exists(nodePath, watcher: null);
                VerifyBytesAreEqual(nodeData, await ringMaster.GetData(nodePath, watcher: null));
                Assert.AreEqual(null, await ringMaster.GetACL(nodePath, stat));
                await ringMaster.SetData(nodePath, nodeData, -1);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that the ACLs on a node are honored for creates.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestCreateAccessControl()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestCreateAccessControl{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestAccessControl.TestPrefix, nodeName);

                // A client with no identity cannot create a node with Acls that only allow access
                // to clients with a certain identity (Digest2).
                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.Create(nodePath, null, new Acl[] { AllowDigest2 }, CreateMode.Persistent),
                    "Auth failure if a client with no identity attempts to create a node that only allows access to Digest2Identity");

                // A client with a different identity (Digest1) cannot create a node with Acls that only allow
                // access to clients with a certain identity (Digest2).
                await ringMaster.SetAuth(Digest1Identity);

                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.Create(nodePath, null, new Acl[] { AllowDigest2 }, CreateMode.Persistent),
                    "Auth failure if a client with Digest1Identity attempts to create a node that only allows access to Digest2Identity");

                // A client with the correct identity can create the node.
                await ringMaster.SetAuth(Digest2Identity);

                await ringMaster.Create(nodePath, null, new Acl[] { AllowDigest2 }, CreateMode.Persistent);

                await ringMaster.Delete(nodePath, -1);

                // A client with an identity that is listed in the Acls can create a node with Acls that allow
                // access to that identity among other identities.
                await ringMaster.Create(nodePath, null, new Acl[] { AllowDigest1, AllowDigest2 }, CreateMode.Persistent);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that the ACLs on a node are honored for operations.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestOperationAccessControl()
        {
            string nodeName = string.Format("$bvt_TestOperationAccessControl{0}", Guid.NewGuid());
            string nodePath = string.Format("{0}/{1}", TestAccessControl.TestPrefix, nodeName);
            string nodeChildPath = nodePath + "/child";

            IStat stat;

            using (var ringMaster = this.ConnectToRingMaster())
            {
                await ringMaster.SetAuth(Digest1Identity);

                await ringMaster.Create(nodePath, null, new Acl[] { AllowDigest1 }, CreateMode.Persistent);
                stat = await ringMaster.Exists(nodePath, watcher: null);

                using (var ringMasterNoIdentity = this.ConnectToRingMaster())
                {
                    await VerifyRingMasterException(
                      RingMasterException.Code.Authfailed,
                      async () => await ringMasterNoIdentity.Exists(nodePath, watcher: null),
                      "Auth failure if a client with no identity attempts Exists operation on a node that only allows access to Digest1Identity");

                    await VerifyRingMasterException(
                       RingMasterException.Code.Authfailed,
                       async () => await ringMasterNoIdentity.GetData(nodePath, watcher: null),
                       "Auth failure if a client with no identity attempts GetData operation on a node that only allows access to Digest1Identity");

                    await VerifyRingMasterException(
                        RingMasterException.Code.Authfailed,
                        async () => await ringMasterNoIdentity.GetACL(nodePath, stat),
                        "Auth failure if a client with no identity attempts GetACL operation on a node that only allows access to Digest1Identity");

                    await VerifyRingMasterException(
                        RingMasterException.Code.Authfailed,
                        async () => await ringMasterNoIdentity.GetChildren(nodePath, watcher: null),
                        "Auth failure if a client with no identity attempts GetChildren operation on a node that only allows access to Digest1Identity");

                    await VerifyRingMasterException(
                       RingMasterException.Code.Authfailed,
                       async () => await ringMasterNoIdentity.SetData(nodePath, null, -1),
                       "Auth failure if a client with no identity attempts SetData operation on a node that only allows access to Digest2Identity");

                    await VerifyRingMasterException(
                      RingMasterException.Code.Authfailed,
                      async () => await ringMasterNoIdentity.SetACL(nodePath, new Acl[] { AllowDigest1 }, -1),
                      "Auth failure if a client with no identity attempts SetACL operation on a node that only allows access to Digest1Identity");

                    await VerifyRingMasterException(
                      RingMasterException.Code.Authfailed,
                      async () => await ringMasterNoIdentity.Create(nodeChildPath, null, null, CreateMode.Persistent),
                      "Auth failure if a client with no identity attempts to create a child node under a node that only allows access to Digest1Identity");

                    await VerifyRingMasterException(
                      RingMasterException.Code.Authfailed,
                      async () => await ringMasterNoIdentity.Sync(nodePath),
                      "Auth failure if a client with no identity attempts Sync operation on a node that only allows access to Digest1Identity");

                    await VerifyRingMasterException(
                       RingMasterException.Code.Authfailed,
                       async () => await ringMasterNoIdentity.Delete(nodePath, -1),
                       "Auth failure if a client with no identity attempts to delete a node that only allows access to Digest1Identity");
                }
            }

            using (var ringMaster = this.ConnectToRingMaster())
            {
                await ringMaster.SetAuth(Digest2Identity);

                await VerifyRingMasterException(
                      RingMasterException.Code.Authfailed,
                      async () => await ringMaster.Exists(nodePath, watcher: null),
                      "Auth failure if a client with Digest2Identity attempts Exists operation on a node that only allows access to Digest1Identity");

                await VerifyRingMasterException(
                   RingMasterException.Code.Authfailed,
                   async () => await ringMaster.GetData(nodePath, watcher: null),
                   "Auth failure if a client with Digest2Identity attempts GetData operation on a node that only allows access to Digest1Identity");

                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.GetACL(nodePath, stat),
                    "Auth failure if a client with Digest2Identity attempts GetACL operation on a node that only allows access to Digest1Identity");

                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.GetChildren(nodePath, watcher: null),
                    "Auth failure if a client with Digest2Identity attempts GetChildren operation on a node that only allows access to Digest1Identity");

                await VerifyRingMasterException(
                   RingMasterException.Code.Authfailed,
                   async () => await ringMaster.SetData(nodePath, null, -1),
                   "Auth failure if a client with Digest2Identity attempts SetData operation on a node that only allows access to Digest2Identity");

                await VerifyRingMasterException(
                  RingMasterException.Code.Authfailed,
                  async () => await ringMaster.SetACL(nodePath, new Acl[] { AllowDigest1 }, -1),
                  "Auth failure if a client with no identity attempts SetACL operation on a node that only allows access to Digest1Identity");

                await VerifyRingMasterException(
                  RingMasterException.Code.Authfailed,
                  async () => await ringMaster.Create(nodeChildPath, null, null, CreateMode.Persistent),
                  "Auth failure if a client with Digest2Identity attempts to create a child node under a node that only allows access to Digest1Identity");

                await VerifyRingMasterException(
                  RingMasterException.Code.Authfailed,
                  async () => await ringMaster.Sync(nodePath),
                  "Auth failure if a client with Digest2Identity attempts Sync operation on a node that only allows access to Digest1Identity");

                await VerifyRingMasterException(
                   RingMasterException.Code.Authfailed,
                   async () => await ringMaster.Delete(nodePath, -1),
                   "Auth failure if a client with Digest2Identity attempts to delete a node that only allows access to Digest1Identity");
            }

            using (var ringMaster = this.ConnectToRingMaster())
            {
                await ringMaster.SetAuth(Digest1Identity);

                // All operations must succeed when the client has Digest1Identity
                stat = await ringMaster.Exists(nodePath, watcher: null);
                Assert.IsNull(await ringMaster.GetData(nodePath, watcher: null));
                VerifyAclListsAreEqual(new Acl[] { AllowDigest1 }, await ringMaster.GetACL(nodePath, stat));
                IReadOnlyList<string> children = await ringMaster.GetChildren(nodePath, watcher: null);
                Assert.AreEqual(0, children.Count);
                await ringMaster.SetData(nodePath, null, -1);
                await ringMaster.SetACL(nodePath, new Acl[] { AllowDigest1 }, -1);
                await ringMaster.Create(nodeChildPath, null, null, CreateMode.Persistent);
                await ringMaster.Sync(nodePath);
                await ringMaster.Delete(nodeChildPath, -1);
                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that the ACLs on a node can be changed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestChangeAccessControl()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestChangeAccessControl{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestAccessControl.TestPrefix, nodeName);

                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                await ringMaster.SetACL(nodePath, new Acl[] { AllowDigest2 }, -1);

                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.SetACL(nodePath, null, -1),
                    "Auth failure if a client with no identity attempts SetACL operation on a node that only allows access to Digest2Identity");

                await ringMaster.SetAuth(Digest1Identity);

                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.SetACL(nodePath, new Acl[] { AllowDigest1 }, -1),
                    "Auth failure if a client with Digest1Identity attempts SetACL operation on a node that only allows access to Digest2Identity");

                await ringMaster.SetAuth(Digest2Identity);

                await ringMaster.Delete(nodePath, -1);
            }
        }

        /// <summary>
        /// Verify that the ACLs on a node can be removed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        public async Task TestRemoveAccessControl()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestRemoveAccessControl{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestAccessControl.TestPrefix, nodeName);

                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);

                await ringMaster.SetACL(nodePath, new Acl[] { AllowDigest2 }, -1);

                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.SetACL(nodePath, null, -1),
                    "Auth failure if a client with no identity attempts SetACL operation on a node that only allows access to Digest2Identity");

                await ringMaster.SetAuth(Digest1Identity);

                await VerifyRingMasterException(
                    RingMasterException.Code.Authfailed,
                    async () => await ringMaster.SetACL(nodePath, new Acl[] { AllowDigest1 }, -1),
                    "Auth failure if a client with Digest1Identity attempts SetACL operation on a node that only allows access to Digest2Identity");

                await ringMaster.SetAuth(Digest2Identity);

                await ringMaster.SetACL(nodePath, null, -1);

                // After the ACL is removed, clients with Digest1 identity or no identity must be able to access the node
                await ringMaster.SetAuth(Digest1Identity);

                Assert.IsNull(await ringMaster.GetData(nodePath, watcher: null));

                using (var ringMasterNoIdentity = this.ConnectToRingMaster())
                {
                    await ringMasterNoIdentity.Delete(nodePath, -1);
                }
            }
        }

        public async Task TestBatchReadAccessControl()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                string nodeName = string.Format("$bvt_TestBatchAccessControl{0}", Guid.NewGuid());
                string nodePath = string.Format("{0}/{1}", TestAccessControl.TestPrefix, nodeName);

                await ringMaster.Create(nodePath, null, null, CreateMode.Persistent);
                await ringMaster.SetACL(nodePath, new Acl[] { AllowDigest1 }, -1);

                await VerifyBatchOperationAuthFailure(ringMaster, Op.GetData(nodePath, RequestGetData.GetDataOptions.None, null));

                await ringMaster.SetAuth(Digest1Identity);

                Assert.IsNull(await ringMaster.GetData(nodePath, watcher: null));

                await VerifyBatchOperationSuccess(ringMaster, Op.GetData(nodePath, RequestGetData.GetDataOptions.None, null));
            }
        }

        private static async Task VerifyBatchOperationSuccess(IRingMasterRequestHandler ringMaster, params Op[] operations)
        {
            IReadOnlyList<OpResult> results = await ringMaster.Batch(operations);

            Assert.AreEqual(operations.Length, results.Count);
            foreach (OpResult result in results)
            {
                Assert.AreEqual(RingMasterException.Code.Ok, result.ErrCode);
            }
        }

        private static async Task VerifyBatchOperationAuthFailure(IRingMasterRequestHandler ringMaster, params Op[] operations)
        {
            IReadOnlyList<OpResult> results = await ringMaster.Batch(operations);

            Assert.AreEqual(operations.Length, results.Count);
            foreach (OpResult result in results)
            {
                Assert.AreEqual(RingMasterException.Code.Authfailed, result.ErrCode);
            }
        }
    }
}