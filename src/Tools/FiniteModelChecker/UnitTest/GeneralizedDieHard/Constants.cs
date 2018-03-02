// <copyright file="Constants.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelCheckerUnitTest.GeneralizedDieHard
{
    using System.Collections.Generic;

    /// <summary>
    /// The values of all constants given as input to the program.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Constants"/> class.
        /// </summary>
        /// <param name="jugs">The list of jugs.</param>
        /// <param name="jugCapacities">The mapping of jugs to their capacity.</param>
        public Constants(List<int> jugs, Dictionary<int, int> jugCapacities)
        {
            this.Jugs = jugs;
            this.JugCapacities = jugCapacities;
        }

        /// <summary>
        /// Gets or sets the list of all jugs.
        /// </summary>
        public List<int> Jugs { get; }

        /// <summary>
        /// Gets or sets the map of jugs to their capacities.
        /// </summary>
        public Dictionary<int, int> JugCapacities { get; }
    }
}