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
        /// <returns>
        /// current vega server
        /// </returns>
        Task<Tuple<string, string>> GetVegaServiceInfo();
    }
}
