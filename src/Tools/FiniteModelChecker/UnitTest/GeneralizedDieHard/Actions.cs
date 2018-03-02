// <copyright file="Actions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Actions to perform on the jugs.
    /// </summary>
    internal class Actions
    {
        /// <summary>
        /// Empties the jug.
        /// </summary>
        /// <param name="jug">The jug to empty.</param>
        /// <param name="jugLevels">The mapping of jug to jug level.</param>
        public static void EmptyJug(int jug, Dictionary<int, int> jugLevels)
        {
            jugLevels[jug] = 0;
        }

        /// <summary>
        /// Fills the jug.
        /// </summary>
        /// <param name="jug">The jug to fill.</param>
        /// <param name="jugCapacities">The mapping of jug to jug capacity.</param>
        /// <param name="jugLevels">The mapping of jug to jug level.</param>
        public static void FillJug(int jug, Dictionary<int, int> jugCapacities, Dictionary<int, int> jugLevels)
        {
            jugLevels[jug] = jugCapacities[jug];
        }

        /// <summary>
        /// Transfers water from one jug to another.
        /// </summary>
        /// <param name="sourceJug">The source jug.</param>
        /// <param name="destinationJug">The destination jug.</param>
        /// <param name="jugCapacities">The mapping of jug to jug capacity.</param>
        /// <param name="jugLevels">The mapping of jug to jug level.</param>
        public static void TransferBetweenJugs(
            int sourceJug,
            int destinationJug,
            Dictionary<int, int> jugCapacities,
            Dictionary<int, int> jugLevels)
        {
            int transferAmount = Math.Min(
                jugLevels[sourceJug],
                jugCapacities[destinationJug] - jugLevels[destinationJug]);
            jugLevels[sourceJug] -= transferAmount;
            jugLevels[destinationJug] += transferAmount;
        }
    }
}