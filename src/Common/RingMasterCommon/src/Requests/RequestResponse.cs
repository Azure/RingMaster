// <copyright file="RequestResponse.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
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

        /// <inheritdoc/>
        public override string ToString()
        {
            var s = this.Stat;
            if (s == null)
            {
                return $"Id:{this.CallId} Code:{this.ResultCode}";
            }
            else
            {
                // Keep the string compact to not overwhelm the log
                return string.Format(
                    "Id:{0} Code:{1} Stat:{2}",
                    this.CallId,
                    this.ResultCode,
                    $"Ver:{s.Version}/{s.Cversion}/{s.Aversion} XID:{s.Czxid}/{s.Mzxid}/{s.Pzxid} Time:{s.Ctime}/{s.Mtime} Data:{s.DataLength} Children:{s.NumChildren}");
            }
        }
    }
}
