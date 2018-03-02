// <copyright file="OpCode.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    /// <summary>
    /// Operation codes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "Simple declaration of Op codes")]
    public enum OpCode : int
    {
        Auth = 1,
        Check,
        CloseSession,
        Create,
        CreateSession,
        Delete,
        Error,
        Exists,
        GetACL,
        GetChildren,
        GetChildren2,
        GetData,
        Multi,
        Notification,
        Ping,
        Sasl,
        SetACL,
        SetData,
        SetWatches,
        Sync,
        Move
    }
}