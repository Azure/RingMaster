// <copyright file="InvariantReport.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    /// <summary>
    /// Report on the checking of a state against an invariant.
    /// </summary>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public class InvariantReport<TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvariantReport{TConstants, TVariables}"/>
        /// class.
        /// </summary>
        /// <param name="invariant">The invariant.</param>
        /// <param name="holds">Whether the invariant holds.</param>
        /// <param name="description">Optional description of failing check.</param>
        public InvariantReport(
            ISafetyInvariant<TConstants, TVariables> invariant,
            bool holds,
            string description = null)
        {
            this.Invariant = invariant;
            this.Holds = holds;
            this.Description = description;
        }

        /// <summary>
        /// Gets the invariant.
        /// </summary>
        public ISafetyInvariant<TConstants, TVariables> Invariant { get; }

        /// <summary>
        /// Gets whether the invariant holds for the state.
        /// </summary>
        public bool Holds { get; }

        /// <summary>
        /// Gets an optional human-readable description of the failing check.
        /// </summary>
        public string Description { get; }
    }
}