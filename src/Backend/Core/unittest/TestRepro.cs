// <copyright file="TestRepro.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that reproduce RingMaster issues and verify that those are fixed.
    /// </summary>
    [TestClass]
    public class TestRepro : RingMasterBackendCoreUnitTest
    {
        /// <summary>
        /// Repro case for VSO 1322955: RingMasterBackendCore: Record resolved incorrectly.
        /// </summary>
        [TestMethod]
        public void TestRecordResolvedIncorrectly()
        {
            Task.Run(async () =>
            {
                using (var ringMaster = this.ConnectToRingMaster())
                {
                    byte[] someData = Guid.NewGuid().ToByteArray();
                    await ringMaster.Create("/test/some/some", someData, null, CreateMode.Persistent | CreateMode.AllowPathCreationFlag);

                    try
                    {
                        // The issue that caused VSO 1322955 was that the data of "/test/some/some" was returned when "/test/some/doesnotexist/doesnotexisttoo" was queried.  This is because
                        // of an internal error which caused the data of a child node with the same name as the last existing parent node (in this case "some") to be read.
                        // The expected behavior is GetData throwing a RingMasterException with code Nonode.
                        byte[] data = await ringMaster.GetData("/test/some/doesnotexist/doesnotexisttoo", RequestGetData.GetDataOptions.FaultbackOnParentData, optionArgument: null, watcher: null);
                        CollectionAssert.AreEqual(someData, data);

                        Assert.Fail("Issue VSO 1322955 has been detected");
                    }
                    catch (RingMasterException ex)
                    {
                        Assert.AreEqual(RingMasterException.Code.Nonode, ex.ErrorCode);
                    }
                }
            }).Wait();
        }
    }
}