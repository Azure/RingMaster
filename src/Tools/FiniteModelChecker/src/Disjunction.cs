// <copyright file="Disjunction.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A disjunction of several sub-next-state-relations.
    /// </summary>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public class Disjunction<TConstants, TVariables>
        : INextStateRelation<TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// The list of all sub-next-state-relations.
        /// </summary>
        private readonly List<INextStateRelation<TConstants, TVariables>> disjuncts;

        /// <summary>
        /// Initializes a new instance of the <see cref="Disjunction{TConstants, TVariables}"/>
        /// class.
        /// </summary>
        /// <param name="disjuncts">The list of all sub-next-state-relations to disjoin.</param>
        public Disjunction(params INextStateRelation<TConstants, TVariables>[] disjuncts)
        {
            this.disjuncts = new List<INextStateRelation<TConstants, TVariables>>(disjuncts);
        }

        /// <inheritdoc/>
        public List<TVariables> GetNextStates(TConstants constants, TVariables current)
        {
            List<TVariables> nextStates = new List<TVariables>();
            foreach (INextStateRelation<TConstants, TVariables> disjunct in this.disjuncts)
            {
                if (disjunct.IsEnabled(constants, current))
                {
                    nextStates.AddRange(disjunct.GetNextStates(constants, current));
                }
            }

            return nextStates;
        }

        /// <inheritdoc/>
        public bool IsEnabled(TConstants constants, TVariables current)
        {
            return this.disjuncts.Any(d => d.IsEnabled(constants, current));
        }
    }
}