// <copyright file="WrapperInterface.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace ApiCoverageCalculator.ApiCoverageCalculateRequestModel
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Information about interface wrapping another interface
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject]
    public class WrapperInterface
    {
        /// <summary>
        /// Gets or sets interface name, full name preferred as it reduced possibility of ambiguous
        /// </summary>
        public string InterfaceName { get; set; }

        /// <summary>
        /// Gets or sets assembly that the interface lives
        /// </summary>
        public string AssemblyName { get; set; }
    }
}
