// <copyright file="IBench.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;

    /// <summary>
    /// Benchmark code common interface
    /// </summary>
    internal interface IBench
    {
        /// <summary>
        /// Runs the benchmark in this suite
        /// </summary>
        /// <param name="log">Logging function</param>
        void Run(Action<string> log);
    }
}
