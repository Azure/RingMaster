// <copyright file="Variables.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FiniteModelChecker;

    /// <summary>
    /// The current values of all variables.
    /// </summary>
    public class Variables : IVariables
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Variables"/> class.
        /// </summary>
        /// <param name="description">The action taken to enter this state.</param>
        public Variables(string description)
        {
            this.JugLevels = new Dictionary<int, int>();
            this.Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variables"/> class.
        /// </summary>
        /// <param name="other">The <see cref="Variables"/> instance from which to copy values.</param>
        /// <param name="description">The action taken to enter this state.</param>
        public Variables(Variables other, string description)
        {
            if (null == other)
            {
                throw new ArgumentNullException(nameof(other));
            }

            this.JugLevels = new Dictionary<int, int>(other.JugLevels);
            this.Description = description;
        }

        /// <summary>
        /// Gets the map of jugs to their current water levels.
        /// </summary>
        public Dictionary<int, int> JugLevels { get; }

        /// <summary>
        /// Gets a description of the action taken to enter the state.
        /// This should NOT be used in calculating the hash code.
        /// </summary>
        public string Description { get; }

        /// <inheritdoc/>
        public long GetLongHashCode()
        {
            long hash = 17;
            foreach (int jugLevel in this.JugLevels.Values)
            {
                hash = (hash * 23) + jugLevel.GetHashCode();
            }

            return hash;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            IEnumerable<string> elements = this.JugLevels.Select(kv => $"{{{kv.Key} : {kv.Value}}}");
            return $"{this.Description} [{string.Join(", ", elements)}]";
        }
    }
}