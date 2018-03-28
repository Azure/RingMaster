// <copyright file="States.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    /// <summary>
    /// Enum States
    /// </summary>
    public enum States
    {
        /// <summary>
        /// The associating
        /// </summary>
        Associating,

        /// <summary>
        /// The aut h_ failed
        /// </summary>
        AuthFailed,

        /// <summary>
        /// The closed
        /// </summary>
        Closed,

        /// <summary>
        /// The connected
        /// </summary>
        Connected,

        /// <summary>
        /// The connecting
        /// </summary>
        Connecting,
    }
}