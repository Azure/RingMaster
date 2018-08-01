// <copyright file="TestServiceDiscovery.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabricUnitTest
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify <see cref="ServiceDiscovery"/> functionality.
    /// </summary>
    [TestClass]
    public class TestServiceDiscovery
    {
        /// <summary>
        /// Verify that a replica's endpoint can be retrieved.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestGetReplicaEndpoint()
        {
            string replicaAddress = "{\"Endpoints\":{\"ServiceEndpoint\":\"TCP://10.0.0.4:99\",\"ManagementEndpoint\":\"net.tcp://10.0.0.6:61019/ManagementEndpoint\"}}";
            var serviceEndpoint = ServiceDiscovery.GetReplicaEndpointUri(replicaAddress, "ServiceEndpoint");
            var managementEndpoint = ServiceDiscovery.GetReplicaEndpointUri(replicaAddress, "ManagementEndpoint");

            Assert.AreEqual("10.0.0.4", serviceEndpoint.Host);
            Assert.AreEqual(99, serviceEndpoint.Port);
            Assert.AreEqual("/", serviceEndpoint.PathAndQuery);

            Assert.AreEqual("10.0.0.6", managementEndpoint.Host);
            Assert.AreEqual(61019, managementEndpoint.Port);
            Assert.AreEqual("/ManagementEndpoint", managementEndpoint.PathAndQuery);
        }
    }
}
