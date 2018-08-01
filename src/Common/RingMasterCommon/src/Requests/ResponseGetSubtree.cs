// <copyright file="ResponseGetSubtree.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using Data;

    /// <summary>
    /// Response of a GetSubtree request.
    /// </summary>
    public struct ResponseGetSubtree
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseGetSubtree"/> struct.
        /// </summary>
        /// <param name="subtree">Subtree data</param>
        /// <param name="continuationPath">Continuation path</param>
        public ResponseGetSubtree(TreeNode subtree, string continuationPath)
        {
            this.ContinuationPath = continuationPath;
            this.Subtree = subtree;
        }

        /// <summary>
        /// Gets the continuation path of the response.
        /// </summary>
        public string ContinuationPath { get; private set; }

        /// <summary>
        /// Gets the subtree data returned in the response.
        /// </summary>
        public TreeNode Subtree { get; private set; }
    }
}
