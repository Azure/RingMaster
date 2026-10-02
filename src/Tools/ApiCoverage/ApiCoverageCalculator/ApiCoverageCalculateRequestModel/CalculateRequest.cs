// <copyright file="CalculateRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace ApiCoverageCalculator.ApiCoverageCalculateRequestModel
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Calculation request for a specific dll
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject]
    public class CalculateRequest
    {
        /// <summary>
        /// Gets or sets assembly dll file name
        /// </summary>
        public string DllFileName { get; set; }

        /// <summary>
        /// Gets or sets absolute path of the coverage file produced by VS
        /// </summary>
        public string CoverageFileName { get; set; }

        /// <summary>
        /// Gets or sets folder that contains the assembly dll and its dependencies
        /// </summary>
        public string AssemblyFolder { get; set; }
    }
}
