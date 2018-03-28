// <copyright file="ISessionAuth.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    /// <summary>
    /// Session authentication data
    /// </summary>
    public interface ISessionAuth
    {
        /// <summary>
        /// Gets a value indicating whether this is a super user session.
        /// </summary>
        bool IsSuperSession { get; }

        /// <summary>
        /// Gets a value indicating whether this session is lock-free even for write operations.
        /// </summary>
        bool IsLockFreeSession { get; }

        /// <summary>
        /// Gets the IP of the client for this session.
        /// </summary>
        string ClientIP { get; }

        /// <summary>
        /// Gets the identity of the client.
        /// </summary>
        string ClientIdentity { get; }

        /// <summary>
        /// Gets the digest given by the client.
        /// </summary>
        string ClientDigest { get; }
    }
}
