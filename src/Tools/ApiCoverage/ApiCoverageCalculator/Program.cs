// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace ApiCoverageCalculator
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Start ApiCoverageCalculator
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        /// <summary>
        /// Entry point of ApiCoverageCalculator
        /// </summary>
        public static void Main()
        {
            ApiCoverageCoordinator acc = new ApiCoverageCoordinator();
            acc.Start();
        }
    }
}
