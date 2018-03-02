// <copyright file="TestFiniteModelChecker.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest
{
    using System;
    using System.Linq;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for the <see cref="FiniteModelChecker"/> module.
    /// </summary>
    [TestClass]
    public class TestFiniteModelChecker
    {
        /// <summary>
        /// Tests the model checker with the <see cref="GeneralizedDieHard"/> module.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGeneralizedDieHard()
        {
            ModelCheckReport<Constants, Variables> report = Models.CheckBasic();
            Console.WriteLine(report);
            Assert.IsFalse(report.Success);
            Assert.AreEqual(typeof(Goal), report.SafetyInvariantsViolated.Single().GetType());

            report = Models.CheckLarge();
            Console.WriteLine(report);
            Assert.IsFalse(report.Success);
            Assert.AreEqual(typeof(Goal), report.SafetyInvariantsViolated.Single().GetType());

            report = Models.CheckImpossible();
            Console.WriteLine(report);
            Assert.IsTrue(report.Success);
        }
    }
}