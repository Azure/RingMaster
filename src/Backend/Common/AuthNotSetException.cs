// <copyright file="AuthNotSetException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;

    /// <summary>
    /// Authentication info is not set
    /// </summary>
    [Serializable]
    public class AuthNotSetException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthNotSetException"/> class.
        /// </summary>
        /// <param name="msg">Exception message</param>
        public AuthNotSetException(string msg)
            : base(msg)
        {
        }
    }
}
