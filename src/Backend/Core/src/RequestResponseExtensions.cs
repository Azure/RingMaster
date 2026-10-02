// <copyright file="RequestResponseExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Contains extension methods for <see cref="RequestResponse"/>
    /// </summary>
    public static class RequestResponseExtensions
    {
        /// <summary>
        /// A more optimal version of <see cref="RequestResponse.ToString"/> implementation.
        /// </summary>
        /// <param name="response">The response</param>
        /// <returns>A string representation of <paramref name="response"/></returns>
        /// <remarks>
        /// String interpolation improvements are available only in net6+ since some interfaces like ISpanFormattable are not available in .netstandard or full framework.
        /// This means that the same interpolated string behaves differently depending on the target framework.
        /// For instance, since this assembly targets both .netstandard2.0 and net6, it would have different code gen for both of them.
        /// And even though we can consume .netstandard2.0 version from net6 application we would have lower performance compared to the case
        /// when net6 app would consume net6 version of this dll.
        /// </remarks>
        public static string ToStringFast(this RequestResponse response)
        {
            var s = response.Stat;
            if (s == null)
            {
                return $"Id: {response.CallId} Code: {response.AsResultCode.ToStringFast()}";
            }
            else
            {
                // Keep the string compact to not overwhelm the log
                return $"Id: {response.CallId} Code: {response.AsResultCode.ToStringFast()} Stat: Ver:{s.Version}/{s.Cversion}/{s.Aversion} XID:{s.Czxid}/{s.Mzxid}/{s.Pzxid} Time:{s.Ctime}/{s.Mtime} Data:{s.DataLength} Children:{s.NumChildren}";
            }
        }
    }
}
