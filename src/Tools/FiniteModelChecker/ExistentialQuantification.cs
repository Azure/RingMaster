// <copyright file="ExistentialQuantification.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A disjunction of next-state relations over a set of values.
    /// </summary>
    /// <typeparam name="TSet">The set of values over which to quantify.</typeparam>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public class ExistentialQuantification<TSet, TConstants, TVariables>
        : INextStateRelation<TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// The instantiated macros.
        /// </summary>
        private readonly Disjunction<TConstants, TVariables> instantiatedMacros;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="ExistentialQuantification{TSet, TConstants, TVariables}"/> class.
        /// </summary>
        /// <param name="set">The set over which to quantify.</param>
        /// <param name="macro">The macro to instantiate with a member of the set.</param>
        public ExistentialQuantification(
            IReadOnlyList<TSet> set,
            Func<TSet, INextStateRelation<TConstants, TVariables>> macro)
        {
            this.instantiatedMacros =
                new Disjunction<TConstants, TVariables>(set.Select(macro).ToArray());
        }

        /// <inheritdoc/>
        public List<TVariables> GetNextStates(TConstants constants, TVariables current)
        {
            return this.instantiatedMacros.GetNextStates(constants, current);
        }

        /// <inheritdoc/>
        public bool IsEnabled(TConstants constants, TVariables current)
        {
            return this.instantiatedMacros.IsEnabled(constants, current);
        }
    }
}