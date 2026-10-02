// <copyright file="ApiVersion.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    /// <summary>
    /// The vega's api version.
    /// </summary>
    public static class ApiVersion
    {
        /// <summary>
        /// The current API version
        /// </summary>
        public const int CurrentApiVersion = Version1;

        /// <summary>
        /// The version that includes Uversion in Stat for GetFullSubtree/GetSubtree request.
        /// </summary>
        public const int Version1 = 1;

        /// <summary>
        /// The API version string
        /// </summary>
        public const string ApiVersionString = "$apiversion";

        /// <summary>
        /// Gets the current API version postfix.
        /// </summary>
        /// <value>
        /// The current API version postfix.
        /// </value>
        public static string CurrentApiVersionPostfix => $"{ApiVersionString}={CurrentApiVersion}$";
    }
}
