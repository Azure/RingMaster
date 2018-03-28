// <copyright file="RequestDelegates.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    /// <summary>
    /// Class RequestDelegates.
    /// </summary>
    public class RequestDelegates
    {
        /// <summary>
        /// Delegate OnAfterComplete
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="resultcode">The resultcode.</param>
        /// <param name="timeInMillis">The time in millis.</param>
        public delegate void OnAfterComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis);
    }
}