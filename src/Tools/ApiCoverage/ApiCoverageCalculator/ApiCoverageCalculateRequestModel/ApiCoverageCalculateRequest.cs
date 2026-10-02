// <copyright file="ApiCoverageCalculateRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace ApiCoverageCalculator.ApiCoverageCalculateRequestModel
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Request Data model for API coverage calculation
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject]
    public class ApiCoverageCalculateRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiCoverageCalculateRequest"/> class
        /// </summary>
        /// <param name="calculateRequests">List of calculation for specific dll implementing APIs</param>
        /// <param name="interfacesToIgnore">List of Interfaces to Ignore</param>
        /// <param name="interfaceWrappers">List of Interfaces (A) whose implementation is merely invoking another interface's (B)</param>
        [JsonConstructor]
        public ApiCoverageCalculateRequest(List<CalculateRequest> calculateRequests, List<string> interfacesToIgnore, Dictionary<string, WrapperInterface> interfaceWrappers)
        {
            this.CalculateRequests = calculateRequests;
            this.InterfacesToIgnore = interfacesToIgnore;
            this.InterfaceWrappers = interfaceWrappers;
        }

        /// <summary>
        /// Gets list of calculation for specific dll implementing APIs
        /// </summary>
        public List<CalculateRequest> CalculateRequests { get; }

        /// <summary>
        /// Gets list of Interfaces to Ignore
        /// </summary>
        public List<string> InterfacesToIgnore { get; }

        /// <summary>
        /// Gets list of Interfaces (A) whose implementation is merely invoking another interface's (B)
        /// In this case, as long as B is covered, count A as covered
        /// </summary>
        public Dictionary<string, WrapperInterface> InterfaceWrappers { get; }
    }
}
