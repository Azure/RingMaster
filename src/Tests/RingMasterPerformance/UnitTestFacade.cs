// <copyright file="UnitTestFacade.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System.IO;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Vega.Test.Helpers;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit test facade so CloudTest can directly run this executable
    /// </summary>
    [TestClass]
    public sealed class UnitTestFacade
    {
        /// <summary>
        /// Setup trace logs at assembly level
        /// </summary>
        /// <param name="context">Test context</param>
        [AssemblyInitialize]
        public static void Setup(TestContext context)
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(assemblyPath)).AddJsonFile("appSettings.json");
            IConfiguration appSettings = builder.Build();

            Helpers.SetupTraceLog(Path.Combine(appSettings["LogFolder"], "RingMasterPerformance.LogPath"));
        }

        /// <summary>
        /// Creates node
        /// </summary>
        [TestMethod]
        public void Create()
        {
            RingMasterPerformance.Main(new[] { "create" });
        }

        /// <summary>
        /// Sets node data
        /// </summary>
        [TestMethod]
        public void SetData()
        {
            RingMasterPerformance.Main(new[] { "setdata" });
        }

        /// <summary>
        /// Creates watchers
        /// </summary>
        [TestMethod]
        public void Watchers()
        {
            RingMasterPerformance.Main(new[] { "watchers" });
        }

        /// <summary>
        /// Gets node data
        /// </summary>
        [TestMethod]
        public void GetData()
        {
            RingMasterPerformance.Main(new[] { "getdata" });
        }
    }
}
