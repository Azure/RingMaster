// <copyright file="RequestGetChildren.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to get the names of the children of a node.
    /// </summary>
    public class RequestGetChildren : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetChildren"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="watcher"><see cref="IWatcher"/> to set on the node (or null)</param>
        /// <param name="retrievalCondition">Retrieval conditions</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestGetChildren(string path, IWatcher watcher, string retrievalCondition, ulong uid = 0)
            : base(RingMasterRequestType.GetChildren, path, uid)
        {
            this.Watcher = watcher;
            this.RetrievalCondition = retrievalCondition;
        }

        /// <summary>
        /// Gets or sets the watcher that will be set on the node.
        /// </summary>
        public IWatcher Watcher { get; set; }

        /// <summary>
        /// Gets the retrieval condition definition for the names of the children to retrieve
        /// </summary>
        /// <remarks>
        /// <c> Retrieval condition is in the form >:[top]:[startingChildName].
        /// valid interval definitions:
        ///
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">::contoso"     means give me all childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </c>
        /// </remarks>
        public string RetrievalCondition { get; private set; }

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
