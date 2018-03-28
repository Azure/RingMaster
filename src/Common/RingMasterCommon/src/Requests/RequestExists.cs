// <copyright file="RequestExists.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to check whether a node exists.
    /// </summary>
    public class RequestExists : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestExists"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="watcher">Watcher to set on the node (or null)</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestExists(string path, IWatcher watcher, ulong uid = 0)
            : base(RingMasterRequestType.Exists, path, uid)
        {
            this.Watcher = watcher;
        }

        /// <summary>
        /// Gets or sets the watcher that will be set on the node.
        /// </summary>
        /// <value>The watcher.</value>
        public IWatcher Watcher { get; set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>true</c> because this request does not modify any data</returns>
        public override bool IsReadOnly()
        {
            return true;
        }
    }
}
