// <copyright file="ZooKeeperRequestType.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// ZooKeeper Request Types
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "These come from Zookeeper")]
    public enum ZooKeeperRequestType
    {
        Notification = 0,
        Create = 1,
        Delete = 2,
        Exists = 3,
        GetData = 4,
        SetData = 5,
        GetACL = 6,
        SetACL = 7,
        GetChildren = 8,
        Sync = 9,
        Ping = 11,
        GetChildren2 = 12,
        Check = 13,
        Multi = 14,
        Create2 = 15,
        Reconfig = 16,
        CheckWatches = 17,
        RemoveWatches = 18,
        CreateContainer = 19,
        DeleteContainer = 20,
        Auth = 100,
        SetWatches = 101,
        Sasl = 102,
        CreateSession = -10,
        CloseSession = -11,
        Error = -1,
    }
}
