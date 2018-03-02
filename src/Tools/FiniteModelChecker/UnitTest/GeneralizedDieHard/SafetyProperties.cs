// <copyright file="SafetyProperties.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System;
    using System.Collections.Generic;
    using FiniteModelChecker;

    /// <summary>
    /// Ensures jug water levels remain valid during model execution.
    /// </summary>
    internal class SafetyProperties : ISafetyInvariant<Constants, Variables>
    {
        /// <inheritdoc/>
        public InvariantReport<Constants, Variables> IsSafe(Constants constants, Variables current)
        {
            if (null == constants)
            {
                throw new ArgumentNullException(nameof(constants));
            }

            if (null == current)
            {
                throw new ArgumentNullException(nameof(current));
            }

            bool allWithinBounds = true;
            foreach (KeyValuePair<int, int> jugLevel in current.JugLevels)
            {
                int jugCapacity = constants.JugCapacities[jugLevel.Key];
                if (jugLevel.Value < 0 || jugLevel.Value > jugCapacity)
                {
                    allWithinBounds = false;
                }
            }

            return new InvariantReport<Constants, Variables>(this, allWithinBounds);
        }
    }
}
