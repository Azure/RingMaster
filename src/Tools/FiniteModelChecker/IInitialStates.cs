// <copyright file="IInitialStates.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    using System.Collections.Generic;

    /// <summary>
    /// The set of initial states.
    /// </summary>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public interface IInitialStates<in TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// Calculates the set of initial system states.
        /// </summary>
        /// <param name="constants">The system constants.</param>
        /// <returns>A list of initial system states.</returns>
        List<TVariables> GetInitialStates(TConstants constants);
    }
}