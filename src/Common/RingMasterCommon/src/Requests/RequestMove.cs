// <copyright file="RequestMove.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to move a node to a new location.
    /// </summary>
    public class RequestMove : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMove"/> class.
        /// </summary>
        /// <param name="pathSrc">Path to the node</param>
        /// <param name="version">version of the node</param>
        /// <param name="pathDst">Path destination, which will become the new parent of the node</param>
        /// <param name="moveMode">Specifies how the node must be moved</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestMove(string pathSrc, int version, string pathDst, MoveMode moveMode, ulong uid = 0)
            : base(RingMasterRequestType.Move, pathSrc, uid)
        {
            this.PathDst = pathDst;
            this.Version = version;
            this.MoveMode = moveMode;
        }

        /// <summary>
        /// Gets a value that specifies the version expected for the source node
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Gets a value that specifies the path that will become the new parent of the moved node
        /// </summary>
        public string PathDst { get; private set; }

        /// <summary>
        /// Gets a value that specifies how the node will be moved.
        /// </summary>
        public MoveMode MoveMode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>false</c> because this request creates a new node</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
