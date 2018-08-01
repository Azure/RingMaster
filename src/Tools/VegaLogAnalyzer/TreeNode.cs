// <copyright file="TreeNode.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.VegaLogAnalyzer
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The treeNode class
    /// </summary>
    public class TreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public TreeNode(long id)
        {
            this.Id = id;
            this.Children = new List<TreeNode>();
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the parent identifier.
        /// </summary>
        /// <value>
        /// The parent identifier.
        /// </value>
        public long ParentId { get; set; } = -1;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number children.
        /// </summary>
        /// <value>
        /// The number children.
        /// </value>
        public int NumChildren { get; set; } = -1;

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        public List<TreeNode> Children { get; }
    }
}
