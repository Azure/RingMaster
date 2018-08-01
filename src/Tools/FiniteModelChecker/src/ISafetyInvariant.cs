// <copyright file="ISafetyInvariant.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    /// <summary>
    /// A formula which must hold in all system states.
    /// </summary>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public interface ISafetyInvariant<TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// Determines whether the invariant is satisfied.
        /// </summary>
        /// <param name="constants">The system constants.</param>
        /// <param name="current">The system variables.</param>
        /// <returns>Report detailing whether invariant holds.</returns>
        InvariantReport<TConstants, TVariables> IsSafe(TConstants constants, TVariables current);
    }
}