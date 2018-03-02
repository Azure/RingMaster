// <copyright file="InitialStateGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System;
    using System.Collections.Generic;
    using FiniteModelChecker;

    /// <summary>
    /// Generates the set of initial jug states (all empty).
    /// </summary>
    internal class InitialStateGenerator : IInitialStates<Constants, Variables>
    {
        /// <inheritdoc/>
        public List<Variables> GetInitialStates(Constants constants)
        {
            if (null == constants)
            {
                throw new ArgumentNullException(nameof(constants));
            }

            List<Variables> initialStates = new List<Variables>();
            Variables initialState = new Variables("Initial state");
            foreach (int jug in constants.Jugs)
            {
                initialState.JugLevels[jug] = 0;
            }

            initialStates.Add(initialState);
            return initialStates;
        }
    }
}