// <copyright file="INextStateRelation.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    using System.Collections.Generic;

    /// <summary>
    /// A next-state relation.
    /// </summary>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public interface INextStateRelation<in TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// Gets all available next states given a current state.
        /// </summary>
        /// <param name="constants">The system constants.</param>
        /// <param name="current">The current system state.</param>
        /// <returns>A list of states which succeed the current state.</returns>
        List<TVariables> GetNextStates(TConstants constants, TVariables current);

        /// <summary>
        /// Whether this next-state relation is enabled for the given state (can generate at least
        /// one successor).
        /// </summary>
        /// <param name="constants">The system constants.</param>
        /// <param name="current">The current system state.</param>
        /// <returns>Whether this next-state relation is enabled.</returns>
        bool IsEnabled(TConstants constants, TVariables current);
    }
}