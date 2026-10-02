// <copyright file="IVegaServiceInfoReader.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The vega service info reader interface
    /// </summary>
    public interface IVegaServiceInfoReader
    {
        /// <summary>
        /// Gets the vega service information.
        /// </summary>
        /// <param name="targetServiceIndex">Index of the target service.</param>
        /// <param name="hostEndpoint">The host endpoint.</param>
        /// <returns>
        /// current vega server
        /// </returns>
        Task<Tuple<string, string>> GetVegaServiceInfo(int targetServiceIndex = 0, string hostEndpoint = "");
    }
}
