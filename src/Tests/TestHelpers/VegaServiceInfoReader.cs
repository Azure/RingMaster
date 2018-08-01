// <copyright file="VegaServiceInfoReader.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The vega service info reader
    /// </summary>
    /// <seealso cref="Microsoft.Vega.Test.Helpers.IVegaServiceInfoReader" />
    public class VegaServiceInfoReader : IVegaServiceInfoReader
    {
        /// <summary>
        /// Gets the vega service information.
        /// </summary>
        /// <returns>
        /// async task
        /// </returns>
        public async Task<Tuple<string, string>> GetVegaServiceInfo()
        {
             return await Helpers.GetVegaServiceInfo();
        }
    }
}
