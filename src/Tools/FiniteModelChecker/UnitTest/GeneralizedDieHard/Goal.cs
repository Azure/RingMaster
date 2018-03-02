// <copyright file="Goal.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System;
    using System.Linq;
    using FiniteModelChecker;

    /// <summary>
    /// Defines the goal as an invariant which will fail upon the goal being reached.
    /// </summary>
    internal class Goal : ISafetyInvariant<Constants, Variables>
    {
        /// <summary>
        /// The goal water level; at least one jug must reach this level.
        /// </summary>
        private readonly int goalLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="Goal"/> class.
        /// </summary>
        /// <param name="goalLevel">The goal water level.</param>
        public Goal(int goalLevel)
        {
            this.goalLevel = goalLevel;
        }

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

            bool goalNotReached = current.JugLevels.All(jugLevel => this.goalLevel != jugLevel.Value);
            return new InvariantReport<Constants, Variables>(this, goalNotReached);
        }
    }
}