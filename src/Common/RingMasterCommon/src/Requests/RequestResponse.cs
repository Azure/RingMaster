// <copyright file="RequestResponse.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class RequestResponse.
    /// </summary>
    [Serializable]
    public class RequestResponse
    {
        /// <summary>
        /// Gets or sets the call identifier.
        /// </summary>
        /// <value>The call identifier.</value>
        public ulong CallId { get; set; }

        /// <summary>
        /// Gets or sets the result code.
        /// </summary>
        /// <value>The result code.</value>
        public int ResultCode { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        public object Content { get; set; }

        /// <summary>
        /// Gets or sets the stat.
        /// </summary>
        /// <value>The stat.</value>
        public IStat Stat { get; set; }

        /// <summary>
        /// Gets or sets the path for the response (in case there was a wildcard involved and the path for the response is not the one in the request.
        /// </summary>
        public string ResponsePath { get; set; }
    }
}