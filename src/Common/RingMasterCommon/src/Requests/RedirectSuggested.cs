// <copyright file="RedirectSuggested.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// RedirectSuggested is a type of content in RequestResponse that indicates that the server is not a primary and calls must be redirected to the suggested connection string.
    /// </summary>
    [Serializable]
    public class RedirectSuggested
    {
        /// <summary>
        /// Gets or sets the connection string to the primary server to which redirection is suggested.
        /// </summary>
        public string SuggestedConnectionString { get; set; }
    }
}