// <copyright file="AuthSchemes.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
#pragma warning disable CS1591, SA1600 // TODO: document every enum items

    /// <summary>
    /// Ways in which the identity of an actor can be specified.
    /// </summary>
    public static class AuthSchemes
    {
        public const string World = "world";
        public const string Auth = "auth";
        public const string Digest = "digest";
        public const string Host = "host";
        public const string Ip = "ip";
    }
#pragma warning restore
}