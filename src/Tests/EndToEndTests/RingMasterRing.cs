// <copyright file="RingMasterRing.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.EndToEndTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Represents a set of <see cref="RingMasterInstance"/>s that form a replication ring.
    /// </summary>
    public sealed class RingMasterRing
    {
        private readonly List<RingMasterInstance> ringMasters = new List<RingMasterInstance>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterRing"/> class.
        /// </summary>
        /// <param name="id">Unique Id of this ring</param>
        /// <param name="memberCount">Number of <see cref="RingMasterInstance"/>s in this ring</param>
        public RingMasterRing(int id, int memberCount = 1)
        {
            Trace.TraceInformation($"RingMasterRing id={id}");
            this.Id = id;

            for (int i = 0; i < memberCount; i++)
            {
                var ringMaster = new RingMasterInstance(i, isPrimary: i == 0);
                this.ringMasters.Add(ringMaster);

                if (i > 0)
                {
                    this.ringMasters[0].RegisterSecondary(ringMaster);
                }
            }
        }

        /// <summary>
        /// Gets the unique id of this ring.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Start the <see cref="RingMasterRing"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task Start()
        {
            Trace.TraceInformation($"RingMasterRing.Start id={this.Id}");

            // Wait for primary to start
            await this.ringMasters[0].Start();

            Trace.TraceInformation($"RingMasterRing.Start-Done id={this.Id}");
        }

        /// <summary>
        /// Connect to the primary <see cref="RingMasterInstance"/> of this ring.
        /// </summary>
        /// <returns>A <see cref="IRingMasterRequestHandler"/> that can be used to interact with the primary of this ring</returns>
        public IRingMasterRequestHandler Connect()
        {
            Trace.TraceInformation($"RingMasterRing.Connect id={this.Id}");

            // Instance 0 is always the primary of the ring
            return this.ringMasters[0].Connect();
        }

        /// <summary>
        /// Get a list of descendants of the given node that is agreed upon by all members of this ring.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <returns>A <see cref="Task"/> that resolves to the verified list of descendants for the ring</returns>
        public async Task<IList<string>> GetVerifiedDescendantsFromMemory(string path)
        {
            var descendantsInPrimary = await this.ringMasters[0].GetDescendantsFromMemory(path);
            Trace.TraceInformation($"RingMasterRing.GetVerifiedDescendantsFromMemory ringId={this.Id}, path={path}, primaryDescendantsCount={descendantsInPrimary.Count}");

            return descendantsInPrimary;
        }

        /// <summary>
        /// Get a list of descendants of the given node from snapshot that is agreed upon by all members of this ring.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <returns>A <see cref="Task"/> that resolves to the verified list of descendants for the ring</returns>
        public async Task<IList<string>> GetVerifiedDescendantsFromSnapshot(string path)
        {
            var descendantsInPrimary = await this.ringMasters[0].GetDescendantsFromSnapshot(path);
            Trace.TraceInformation($"RingMasterRing.GetVerifiedDescendantsFromSnapshot ringId={this.Id}, path={path}, primaryDescendantsCount={descendantsInPrimary.Count}");

            for (int i = 1; i < this.ringMasters.Count; i++)
            {
                var descendantsInSecondary = await this.ringMasters[i].GetDescendantsFromSnapshot(path);
                Trace.TraceInformation($"RingMasterRing.GetVerifiedDescendantsFromSnapshot ringId={this.Id}, ringMasterInstance={i}, path={path}, secondaryDescendantsCount={descendantsInPrimary.Count}");
                CollectionAssert.AreEqual((ICollection)descendantsInPrimary, (ICollection)descendantsInSecondary);
            }

            return descendantsInPrimary;
        }

        /// <summary>
        /// Verify that the snapshots produced by all ringmaster instances are identical
        /// </summary>
        /// <param name="path">Path for the comparison</param>
        /// <param name="expectedNodeCount">Expected number of nodes</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public async Task VerifyRingMasterDataConsistency(string path, int expectedNodeCount)
        {
            // Verify that the snapshot data matches the in-memory data.
            var descendantsInPrimary = await this.ringMasters[0].GetDescendantsFromMemory(path);

            Assert.AreEqual(expectedNodeCount, descendantsInPrimary.Count);

            var verifiedNodeListFromSnapshot = await this.GetVerifiedDescendantsFromSnapshot(path);

            CollectionAssert.AreEqual((ICollection)descendantsInPrimary, (ICollection)verifiedNodeListFromSnapshot);
        }

        /// <summary>
        /// Verify that all RingMaster instances in this ring produce the same snapshot.
        /// </summary>
        public void VerifyRingMasterSnapshots()
        {
            Trace.TraceInformation("VerifyRingMasterSnapshots");
            var primarySnapshot = this.ringMasters[0].EnumerateFromSnapshot();

            for (int i = 1; i < this.ringMasters.Count; i++)
            {
                Trace.TraceInformation($"VerifyRingMasterSnapshots-ComparingSecondarySnapshotWithPrimary  instanceId={i}");
                var secondarySnapshot = this.ringMasters[i].EnumerateFromSnapshot();
                ulong totalVerified = 0;

                foreach (var pair in primarySnapshot.Zip(secondarySnapshot, (p, s) => new { Primary = p, Secondary = s }))
                {
                    var expected = pair.Primary;
                    var actual = pair.Secondary;

                    Assert.AreEqual(expected.Id, actual.Id, "Id");
                    Assert.AreEqual(expected.Name, actual.Name, "Name");
                    if ((expected.ParentId != 0) && (expected.ParentId != ulong.MaxValue))
                    {
                        Assert.AreEqual(expected.ParentId, actual.ParentId, "ParentId");
                    }

                    Assert.AreEqual(expected.Stat.Czxid, actual.Stat.Czxid, "Czxid");
                    Assert.AreEqual(expected.Stat.Mzxid, actual.Stat.Mzxid, "Mzxid");
                    Assert.AreEqual(expected.Stat.Pzxid, actual.Stat.Pzxid, "Pzxid");
                    Assert.AreEqual(expected.Stat.Version, actual.Stat.Version, "Version");
                    Assert.AreEqual(expected.Stat.Cversion, actual.Stat.Cversion, "Cversion");
                    Assert.AreEqual(expected.Stat.Aversion, actual.Stat.Aversion, "Aversion");

                    if (expected.Data != null)
                    {
                        Assert.IsNotNull(actual.Data);
                        Assert.IsTrue(Backend.HelperTypes.EqualityHelper.Equals(expected.Data, actual.Data), "Data");
                    }
                    else
                    {
                        Assert.IsNull(actual.Data);
                    }

                    if (expected.Acl != null)
                    {
                        Assert.IsNotNull(actual.Acl);
                        Assert.AreEqual(expected.Acl.Count, actual.Acl.Count, "AclCount");
                    }
                    else
                    {
                        Assert.IsNull(actual.Acl);
                    }

                    totalVerified++;
                }

                Assert.AreNotEqual(0, totalVerified);
                Assert.AreEqual(this.ringMasters[0].TotalNodes, totalVerified, "TotalVerified");
                Trace.TraceInformation($"VerifyRingMasterSnapshots-ComparedSecondarySnapshotWithPrimary  instanceId={i}, totalVerified={totalVerified}");
            }
        }

        /// <summary>
        /// Verify that all RingMaster instances in this ring have applied the same change lists in the same order.
        /// </summary>
        public void VerifyCommittedChangeLists()
        {
            Trace.TraceInformation("VerifyCommittedChangeLists");
            var primaryCommittedChangeLists = this.ringMasters[0].CommittedChangeLists;

            for (int instanceIndex = 1; instanceIndex < this.ringMasters.Count; instanceIndex++)
            {
                var secondaryCommittedChangeLists = this.ringMasters[instanceIndex].CommittedChangeLists;

                Trace.TraceInformation($"VerifyCommittedChangeLists-ComparingSecondaryWithPrimary instanceId={instanceIndex}, primaryCommittedChangeListsCount={primaryCommittedChangeLists.Count}");

                Assert.AreEqual(primaryCommittedChangeLists.Count, secondaryCommittedChangeLists.Count);

                for (int changeListIndex = 0; changeListIndex < primaryCommittedChangeLists.Count; changeListIndex++)
                {
                    var primaryChangeList = primaryCommittedChangeLists[changeListIndex];
                    var secondaryChangeList = secondaryCommittedChangeLists[changeListIndex];

                    Assert.AreEqual(primaryChangeList.Id, secondaryChangeList.Id);
                    Assert.AreEqual(primaryChangeList.Changes.Count, secondaryChangeList.Changes.Count);

                    for (int changeIndex = 0; changeIndex < primaryChangeList.Changes.Count; changeIndex++)
                    {
                        var primaryChange = primaryChangeList.Changes[changeIndex];
                        var secondaryChange = secondaryChangeList.Changes[changeIndex];

                        Assert.AreEqual(primaryChange.ChangeType, secondaryChange.ChangeType);
                        Assert.AreEqual(primaryChange.Data.Id, secondaryChange.Data.Id);
                        Assert.AreEqual(primaryChange.Data.Name, secondaryChange.Data.Name);
                    }
                }

                Trace.TraceInformation($"VerifyCommittedChangeLists-ComparedSecondaryWithPrimary instanceId={instanceIndex}, primaryCommittedChangeListsCount={primaryCommittedChangeLists.Count}");
            }
        }
    }
}