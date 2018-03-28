// <copyright file="LimitTests.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Backend;
    using Backend.Persistence;
    using FluentAssertions;
    using Persistence;
    using Persistence.InMemory;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NodeTests
    {
        private readonly AbstractPersistedDataFactory persistedDataFactory = new InMemoryFactory();

        private ulong persistedDataIdCount;

        [TestMethod]
        public void Node_Constructor_Should_Attach_Node_To_Persisted_Data()
        {
            var persistedData = CreatePersistedData("p");
            var node = new Node(persistedData);

            node.Persisted.Should().Be(persistedData, "Node should attach itself to the persisted data");
        }

        [TestMethod]
        public void Node_AddChildren_Should_Upconvert_To_CompleteNode()
        {
            var parentPersistedData = CreatePersistedData("p");
            var parentNode = new Node(parentPersistedData);

            var childrenToAdd = new List<IPersistedData> { CreatePersistedData("c1"), CreatePersistedData("c2") };

            parentNode.AddChildren(childrenToAdd);

            parentPersistedData.Node.Should().BeOfType(typeof(CompleteNode), "Node should be upconverted to CompleteNode since Node cannot have children");

            parentNode = parentPersistedData.Node;
            parentNode.ChildrenCount.Should().Be(2);
            parentNode.ChildrenMapping.Select(kvp => kvp).Should().BeEquivalentTo(childrenToAdd.Select(p => new KeyValuePair<string, IPersistedData>(p.Name, p)), "Children should be added to the parent's children mapping");
        }

        [TestMethod]
        public void Node_AddChildren_Should_Throw_On_Null_List()
        {
            var persistedData = CreatePersistedData("p");
            var node = new Node(persistedData);

            Action addNullChildrenAction = () => node.AddChildren(null);
            addNullChildrenAction.ShouldThrow<ArgumentNullException>("Null children list is not allowed");
        }

        [TestMethod]
        public void Node_AddChildren_Should_Throw_On_Null_PersistedData()
        {
            var persistedData = CreatePersistedData("p");
            var node = new Node(persistedData);

            Action addNullChildrenAction = () => node.AddChildren(new List<IPersistedData> { null });
            addNullChildrenAction.ShouldThrow<ArgumentException>("Null child persisted data is not allowed");
        }

        [TestMethod]
        public void Node_AddChildren_Should_Throw_On_Null_PersistedData_Name()
        {
            var persistedData = CreatePersistedData("p");
            var node = new Node(persistedData);

            Action addNullChildrenAction = () => node.AddChildren(new List<IPersistedData> { CreatePersistedData(null) });
            addNullChildrenAction.ShouldThrow<ArgumentException>("Persisted data with null child name is not allowed");
        }

        private PersistedData CreatePersistedData(string name)
        {
            return new PersistedData(this.persistedDataIdCount++, this.persistedDataFactory)
            {
                Name = name
            };
        }
    }
}
