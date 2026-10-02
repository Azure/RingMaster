// <copyright file="TestNodeContext.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The test node context
    /// </summary>
    public class TestNodeContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestNodeContext"/> class.
        /// </summary>
        /// <param name="testNodeId">The test node identifier.</param>
        /// <param name="targetServiceIndex">Index of the target service.</param>
        public TestNodeContext(int testNodeId, int targetServiceIndex)
        {
            this.TestNodeId = testNodeId;
            this.TargetServiceIndex = targetServiceIndex;
        }

        /// <summary>
        /// Gets or sets the test node identifier.
        /// </summary>
        /// <value>
        /// The test node identifier.
        /// </value>
        public int TestNodeId { get; set; }

        /// <summary>
        /// Gets or sets the index of the target service.
        /// </summary>
        /// <value>
        /// The index of the target service.
        /// </value>
        public int TargetServiceIndex { get; set; }
    }
}
