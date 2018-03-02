// <copyright file="TransferBetweenJugs.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System;
    using System.Collections.Generic;
    using FiniteModelChecker;

    /// <summary>
    /// Next-state relation which transfers water from one jug to the other.
    /// </summary>
    internal class TransferBetweenJugs : INextStateRelation<Constants, Variables>
    {
        /// <summary>
        /// The jug from which to transfer water.
        /// </summary>
        private int sourceJug;

        /// <summary>
        /// The jug to which to transfer water.
        /// </summary>
        private int destinationJug;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransferBetweenJugs"/> class.
        /// </summary>
        /// <param name="sourceJug">The source jug.</param>
        /// <param name="destinationJug">The destination jug.</param>
        public TransferBetweenJugs(int sourceJug, int destinationJug)
        {
            this.sourceJug = sourceJug;
            this.destinationJug = destinationJug;
        }

        /// <inheritdoc/>
        public List<Variables> GetNextStates(Constants constants, Variables current)
        {
            if (null == constants)
            {
                throw new ArgumentNullException(nameof(constants));
            }

            if (null == current)
            {
                throw new ArgumentNullException(nameof(current));
            }

            List<Variables> nextStates = new List<Variables>();
            Variables nextState = new Variables(current, this.ToString());
            Actions.TransferBetweenJugs(this.sourceJug, this.destinationJug, constants.JugCapacities, nextState.JugLevels);
            nextStates.Add(nextState);
            return nextStates;
        }

        /// <inheritdoc/>
        public bool IsEnabled(Constants constants, Variables current)
        {
            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Transfer from jug [{this.sourceJug}] to [{this.destinationJug}]";
        }
    }
}