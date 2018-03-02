// <copyright file="FillJug.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System;
    using System.Collections.Generic;
    using FiniteModelChecker;

    /// <summary>
    /// Next-state relation which fills a jug.
    /// </summary>
    internal class FillJug : INextStateRelation<Constants, Variables>
    {
        /// <summary>
        /// The jug to fill.
        /// </summary>
        private readonly int jug;

        /// <summary>
        /// Initializes a new instance of the <see cref="FillJug"/> class.
        /// </summary>
        /// <param name="jug">The jug to fill.</param>
        public FillJug(int jug)
        {
            this.jug = jug;
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
            Actions.FillJug(this.jug, constants.JugCapacities, nextState.JugLevels);
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
            return $"Fill jug [{this.jug}]";
        }
    }
}