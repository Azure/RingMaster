// <copyright file="AuthSchemes.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// Ways in which the identity of an actor can be specified.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Simple declaration of AuthSchemes")]
    public static class AuthSchemes
    {
        public const string World = "world";
        public const string Auth = "auth";
        public const string Digest = "digest";
        public const string Host = "host";
        public const string Ip = "ip";
    }
}