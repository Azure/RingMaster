// <copyright file="InvalidAclException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// ACL is invalid
    /// </summary>
    [Serializable]
    public class InvalidAclException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidAclException"/> class.
        /// </summary>
        /// <param name="msg">Exception message</param>
        /// <param name="sessionData">Session data</param>
        public InvalidAclException(string msg, string sessionData)
            : base($"Session:{sessionData}, {msg}")
        {
            this.SessionData = sessionData;
        }

        /// <summary>
        /// Gets the session data
        /// </summary>
        public string SessionData { get; }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("SessionData", this.SessionData);
        }
    }
}
