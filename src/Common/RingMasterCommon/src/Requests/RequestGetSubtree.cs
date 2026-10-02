// <copyright file="RequestGetSubtree.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// Request to get the data of the subtree under a node.
    /// </summary>
    public class RequestGetSubtree : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetSubtree"/> class.
        /// </summary>
        /// <param name="path">Path of the subtree to get.</param>
        /// <param name="retrievalCondition">Request retreival condition.</param>
        /// <param name="uid">Request unique identifer.</param>
        public RequestGetSubtree(string path, string retrievalCondition, ulong uid = 0)
            : this(path, retrievalCondition, GetSubtreeOptions.None, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetSubtree"/> class.
        /// </summary>
        /// <param name="path">Path of the subtree to get.</param>
        /// <param name="retrievalCondition">Request retreival condition.</param>
        /// <param name="options">Request options.</param>
        /// <param name="uid">Request unique identifer.</param>
        public RequestGetSubtree(string path, string retrievalCondition, GetSubtreeOptions options, ulong uid = 0)
            : base(RingMasterRequestType.GetSubtree, path, uid)
        {
            this.Options = options;
            this.RetrievalCondition = retrievalCondition;
        }

        /// <summary>
        /// Options for get subtree. Making it starts from 2 to differentiate it from the old boolean value after serialization.
        /// This is essential to make the ringmaster client backward compatible
        /// </summary>
        [Flags]
        public enum GetSubtreeOptions : byte
        {
            /// <summary>
            /// No options.
            /// </summary>
            None = 1 << 1,

            /// <summary>
            /// Include stats for each node in the subtree.
            /// </summary>
            IncludeStats = 1 << 2,

            /// <summary>
            /// Include user metadata for each node in the subtree.
            /// </summary>
            IncludeUserMetadata = 1 << 3,
        }

        /// <summary>
        /// Gets a value indicating whether the result should contain stats.
        /// </summary>
        public bool IncludeStats
        {
            get
            {
                return this.Options.HasFlag(GetSubtreeOptions.IncludeStats);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result should include user metadata.
        /// </summary>
        public bool IncludeUserMetadata
        {
            get
            {
                return this.Options.HasFlag(GetSubtreeOptions.IncludeUserMetadata);
            }
        }

        /// <summary>
        /// Gets the options for this request.
        /// </summary>
        public GetSubtreeOptions Options { get; private set; }

        /// <summary>
        /// Gets the retrieval condition for this request.
        /// </summary>
        public string RetrievalCondition { get; private set; }

        /// <summary>
        /// Gets whether this request is read-only.
        /// </summary>
        /// <returns><c>true</c></returns>
        public override bool IsReadOnly()
        {
            return true;
        }
    }
}
