// <copyright file="ModelCheck.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Finite model-checking of a system.
    /// </summary>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public static class ModelCheck<TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// Performs a breadth-first search of all possible system states.
        /// </summary>
        /// <param name="constants">The system constants.</param>
        /// <param name="init">The system's initial state generator.</param>
        /// <param name="nextStateRelation">The system's next-state relation.</param>
        /// <param name="safetyInvariants">The system's safety invariants.</param>
        /// <returns>A report on the result of the model check.</returns>
        public static ModelCheckReport<TConstants, TVariables> CheckModel(
            TConstants constants,
            IInitialStates<TConstants, TVariables> init,
            INextStateRelation<TConstants, TVariables> nextStateRelation,
            params ISafetyInvariant<TConstants, TVariables>[] safetyInvariants)
        {
            // Validates parameters
            if (null == constants)
            {
                throw new ArgumentNullException(nameof(constants));
            }

            if (null == init)
            {
                throw new ArgumentNullException(nameof(init));
            }

            if (null == nextStateRelation)
            {
                throw new ArgumentNullException(nameof(nextStateRelation));
            }

            // Generates the set of initial states.
            List<TVariables> initialStates = init.GetInitialStates(constants);
            long initialStateCount = initialStates.Count;
            long totalStates = initialStateCount;

            // Initializes the predecessor adjacency list with the initial states.
            long zeroStateHash = 0;
            Dictionary<long, long> predecessors = new Dictionary<long, long>();
            foreach (TVariables initialState in initialStates)
            {
                predecessors[initialState.GetLongHashCode()] = zeroStateHash;
            }

            // Pushes initial states onto a queue and initiates a breadth-first search.
            Queue<TVariables> queue = new Queue<TVariables>(initialStates);
            while (queue.Any())
            {
                // Gets current state and checks it against all safety invariants.
                TVariables currentState = queue.Dequeue();
                long currentStateHash = currentState.GetLongHashCode();
                List<InvariantReport<TConstants, TVariables>> violatedInvariants =
                    safetyInvariants.Select(
                        i => i.IsSafe(constants, currentState)).Where(
                            r => !r.Holds).ToList();
                if (violatedInvariants.Any())
                {
                    // Safety invariant(s) violated; derive & return a state execution trace for the report.
                    List<TVariables> trace = ReconstructStateTransitions(
                        constants,
                        init,
                        nextStateRelation,
                        predecessors,
                        currentStateHash,
                        zeroStateHash);

                    return new ModelCheckReport<TConstants, TVariables>(
                        false,
                        initialStateCount,
                        totalStates,
                        predecessors.Count,
                        violatedInvariants,
                        currentState,
                        trace);
                }

                // Generates the set of all states reachable from the current state.
                List<TVariables> nextStates = nextStateRelation.GetNextStates(constants, currentState);
                totalStates += nextStates.Count;
                foreach (TVariables nextState in nextStates)
                {
                    // If next state has not yet been visited, add it to the search queue.
                    long nextStateHash = nextState.GetLongHashCode();
                    if (!predecessors.ContainsKey(nextStateHash))
                    {
                        predecessors[nextStateHash] = currentStateHash;
                        queue.Enqueue(nextState);
                    }
                }
            }

            // Breadth-first search has successfully terminated without any safety invariants failing.
            return new ModelCheckReport<TConstants, TVariables>(
                initialStateCount,
                totalStates,
                predecessors.Count);
        }

        /// <summary>
        /// Given a predecessor adjacency list, reconstruct the shortest path from initial to goal state.
        /// </summary>
        /// <param name="constants">The system constants.</param>
        /// <param name="init">The system's initial state generator.</param>
        /// <param name="nextStateRelation">The system's next state relation.</param>
        /// <param name="predecessors">The predecessor adjacency list.</param>
        /// <param name="goalStateHash">The hash of the state to which to construct the shortest path.</param>
        /// <param name="zeroStateHash">The hash of the pre-initial state.</param>
        /// <returns>A list of states leading from the initial state to the goal state.</returns>
        private static List<TVariables> ReconstructStateTransitions(
            TConstants constants,
            IInitialStates<TConstants, TVariables> init,
            INextStateRelation<TConstants, TVariables> nextStateRelation,
            Dictionary<long, long> predecessors,
            long goalStateHash,
            long zeroStateHash)
        {
            List<long> stateTransitionHashes = new List<long>();
            long predecessorHash = goalStateHash;
            while (zeroStateHash != predecessorHash)
            {
                stateTransitionHashes.Add(predecessorHash);
                predecessorHash = predecessors[predecessorHash];
            }

            stateTransitionHashes.Reverse();
            List<TVariables> nextStates = init.GetInitialStates(constants);
            List<TVariables> stateTransitions = new List<TVariables>();
            foreach (long currentStateHash in stateTransitionHashes)
            {
                TVariables nextState = nextStates.Single(s => currentStateHash == s.GetLongHashCode());
                stateTransitions.Add(nextState);
                nextStates = nextStateRelation.GetNextStates(constants, nextState);
            }

            return stateTransitions;
        }
    }
}