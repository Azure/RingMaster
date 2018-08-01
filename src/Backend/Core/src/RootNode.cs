// <copyright file="RootNode.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// This is a quite complex trick to save state in the case where the vast majority of nodes are leaves (i.e. no children)
    /// and with no Watchers (typically Watchers will be set on parents).
    /// The trick is as follows: "Node" doesn't have storage members for neither "children" nor "Watchers". "CompleteNode" does have both storage members.
    /// When a 'node' is created, CompleteNode.CreateNode must be used. that will decide wether "Node" or "CompleteNode" are required, based on the
    /// persisted data pd. if pd doesn't need children nor Watchers, then a "Node" is instantiated, otherwise, "CompleteNode" is instantiated.
    /// Later, upon Node.AddChild will create on the fly a CompleteNode to host the children collection, and will relink persistent data to the CompleteNode.
    /// similarly happens with Node.AddWatcher().
    /// Finally, CompleteNode.ClearWatchers() and CompleteNode.RemoveChild(...) might decide to revert a CompleteNode into a Node if the extra space is not neeeded.
    /// A special case of CompleteNode is RootNode, which never springs back to "Node".
    /// All happens due to the fact that PersistedData has the "node" linked from below. All connections are at the level of
    /// the PersistentData (note for example that "childrenNodes" is a dictionary pointing to PersistentData, not Nodes).
    /// </summary>
    public class RootNode : CompleteNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RootNode"/> class.
        /// </summary>
        /// <param name="persisted">The persisted data.</param>
        public RootNode(IPersistedData persisted)
            : base(persisted)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public override bool IsRoot
        {
            get { return true; }
        }

        /// <summary>
        /// Revisits the type of the node based on its need for children and watchers.
        /// </summary>
        /// <param name="makeComplete">if set to <c>true</c> the caller demands a 'complete' node.</param>
        /// <returns>new Node to use</returns>
        protected override Node RevisitNodeType(bool makeComplete = false)
        {
            // the root node must always be a complete node.
            return this.Persisted.Node;
        }
    }
}
