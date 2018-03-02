// <copyright file="Models.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System.Collections.Generic;
    using FiniteModelChecker;

    /// <summary>
    /// Runs various models.
    /// </summary>
    public static class Models
    {
        /// <summary>
        /// The basic problem, like from the movie.
        /// </summary>
        /// <returns>The model check result.</returns>
        public static ModelCheckReport<Constants, Variables> CheckBasic()
        {
            List<int> jugs = new List<int> { 1, 2 };
            Dictionary<int, int> jugCapacities = new Dictionary<int, int>
            {
                [jugs[0]] = 3,
                [jugs[1]] = 5
            };

            Constants constants = new Constants(jugs, jugCapacities);
            IInitialStates<Constants, Variables> init = new InitialStateGenerator();
            INextStateRelation<Constants, Variables> nextStateRelation = BuildNextStateRelation(constants);
            ISafetyInvariant<Constants, Variables> safetyInvariant = new SafetyProperties();
            ISafetyInvariant<Constants, Variables> goal = new Goal(4);

            return ModelCheck<Constants, Variables>.CheckModel(
                constants,
                init,
                nextStateRelation,
                safetyInvariant,
                goal);
        }

        /// <summary>
        /// Model-checks a scenario with more jugs.
        /// </summary>
        /// <returns>The model check result.</returns>
        public static ModelCheckReport<Constants, Variables> CheckLarge()
        {
            List<int> jugs = new List<int> { 1, 2, 3, 4 };
            Dictionary<int, int> jugCapacities = new Dictionary<int, int>
            {
                [jugs[0]] = 3,
                [jugs[1]] = 5,
                [jugs[2]] = 7,
                [jugs[3]] = 9
            };

            Constants constants = new Constants(jugs, jugCapacities);
            IInitialStates<Constants, Variables> init = new InitialStateGenerator();
            INextStateRelation<Constants, Variables> nextStateRelation = BuildNextStateRelation(constants);
            ISafetyInvariant<Constants, Variables> safetyInvariant = new SafetyProperties();
            ISafetyInvariant<Constants, Variables> goal = new Goal(8);

            return ModelCheck<Constants, Variables>.CheckModel(
                constants,
                init,
                nextStateRelation,
                safetyInvariant,
                goal);
        }

        /// <summary>
        /// Model-checks a scenario where the goal is not reachable.
        /// </summary>
        /// <returns>The model check result.</returns>
        public static ModelCheckReport<Constants, Variables> CheckImpossible()
        {
            List<int> jugs = new List<int> { 1, 2, 3 };
            Dictionary<int, int> jugCapacities = new Dictionary<int, int>
            {
                [jugs[0]] = 2,
                [jugs[1]] = 4,
                [jugs[2]] = 8,
            };

            Constants constants = new Constants(jugs, jugCapacities);
            IInitialStates<Constants, Variables> init = new InitialStateGenerator();
            INextStateRelation<Constants, Variables> nextStateRelation = BuildNextStateRelation(constants);
            ISafetyInvariant<Constants, Variables> safetyInvariant = new SafetyProperties();
            ISafetyInvariant<Constants, Variables> goal = new Goal(3);

            return ModelCheck<Constants, Variables>.CheckModel(
                constants,
                init,
                nextStateRelation,
                safetyInvariant,
                goal);
        }

        /// <summary>
        /// Builds the full next-state relation.
        /// </summary>
        /// <param name="constants">The system constants.</param>
        /// <returns>The full next-state relation.</returns>
        private static INextStateRelation<Constants, Variables> BuildNextStateRelation(Constants constants)
        {
            var fillJug =
                new ExistentialQuantification<int, Constants, Variables>(
                    constants.Jugs, jug => new FillJug(jug));
            var emptyJug =
                new ExistentialQuantification<int, Constants, Variables>(
                    constants.Jugs, jug => new EmptyJug(jug));
            var transfer = new ExistentialQuantification<int, Constants, Variables>(
                constants.Jugs,
                jug1 => new ExistentialQuantification<int, Constants, Variables>(
                    constants.Jugs,
                    jug2 => new TransferBetweenJugs(jug1, jug2)));

            return new Disjunction<Constants, Variables>(fillJug, emptyJug, transfer);
        }
    }
}