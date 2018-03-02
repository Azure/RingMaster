// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="ChangeKind.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Enum ChangeKind
    /// </summary>
    public enum ChangeKind : byte
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0,
        /// <summary>
        /// The node created
        /// </summary>
        NodeCreated,
        /// <summary>
        /// The children added
        /// </summary>
        ChildrenAdded,
        /// <summary>
        /// The children removed
        /// </summary>
        ChildrenRemoved,
        /// <summary>
        /// The data changed
        /// </summary>
        DataChanged,
        /// <summary>
        /// The acl changed
        /// </summary>
        AclChanged,
        /// <summary>
        /// The node deleted
        /// </summary>
        NodeDeleted
    }
}