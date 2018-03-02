// <copyright file="RequestCreate.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to create a new node.
    /// </summary>
    public class RequestCreate : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCreate"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="data">Data that will be associated with the newly created node</param>
        /// <param name="acl">List of <see cref="Acl"/>s that will be associated with the newly created node</param>
        /// <param name="createMode">Specifies how the node must be created</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestCreate(string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode, ulong uid = 0)
            : base(RingMasterRequestType.Create, path, uid)
        {
            this.Data = data;
            this.Acl = acl;
            this.CreateMode = createMode;
        }

        /// <summary>
        /// Gets or sets the content that will be stored in the node when it is created.
        /// </summary>
        /// <value>The data.</value>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Acl"/>s that will be associated with the node when it is created.
        /// </summary>
        public IReadOnlyList<Acl> Acl { get; set; }

        /// <summary>
        /// Gets or sets a value that specifies how the node will be created. 
        /// </summary>
        public CreateMode CreateMode { get; set; }

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
