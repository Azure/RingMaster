// <copyright file="RetriableOperationException.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;

    /// <summary>
    /// abstracts an exception that causes this operation to fail, but it is likely to succeed if tried again
    /// </summary>
    [Serializable]
    public class RetriableOperationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetriableOperationException"/> class.
        /// </summary>
        /// <param name="msg">Exception message</param>
        public RetriableOperationException(string msg)
            : base(msg)
        {
        }
    }
}
