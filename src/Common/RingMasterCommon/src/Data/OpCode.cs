// <copyright file="OpCode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
#pragma warning disable SA1602, CS1591 // TODO: document enum items
    /// <summary>
    /// Operation codes.
    /// </summary>
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
        Move,
    }
#pragma warning restore
}