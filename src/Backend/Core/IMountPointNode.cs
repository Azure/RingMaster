// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="IMountPointNode.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using System.Net;

    public interface IMountPointNode
    {
        IPersistedData MountedNodePath { get; }

        void Close();
    }

}