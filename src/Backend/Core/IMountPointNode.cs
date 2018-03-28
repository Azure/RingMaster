// <copyright file="IMountPointNode.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
#pragma warning disable CS1591, SA1600 // add documentation later
    public interface IMountPointNode
    {
        IPersistedData MountedNodePath { get; }

        void Close();
    }
#pragma warning restore
}
