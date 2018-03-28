// <copyright file="TreeNode.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Represents a node in the ringmaster tree.
    /// </summary>
    public class TreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode"/> class.
        /// </summary>
        /// <param name="name">Name of the node</param>
        /// <param name="data">Data associated with the node</param>
        /// <param name="stat">Stat of the node</param>
        /// <param name="children">Children of the node</param>
        private TreeNode(string name, byte[] data, Stat stat, IReadOnlyList<TreeNode> children)
        {
            this.Name = name;
            this.Data = data;
            this.Stat = stat;
            this.Children = children;
        }

        /// <summary>
        /// Gets the name of the node.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the list of children associated with the node.
        /// </summary>
        public IReadOnlyList<TreeNode> Children { get; private set; }

        /// <summary>
        /// Gets the data associated with the node.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the stat of the node
        /// </summary>
        public Stat Stat { get; private set; }

        /// <summary>
        /// Deserializes the node tree that is serialized in the given buffer
        /// </summary>
        /// <param name="buffer">The buffer with the serialized node tree</param>
        /// <returns>The root of the deserialized node tree</returns>
        internal static TreeNode Deserialize(byte[] buffer)
        {
            MemoryStream memoryStream = null;
            try
            {
                memoryStream = new MemoryStream(buffer);
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    memoryStream = null;
                    return TreeNode.Deserialize(reader);
                }
            }
            finally
            {
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Deserializes the node tree that is serialized in a stream.
        /// </summary>
        /// <param name="reader">A <see cref="BinaryReader"/> that can be used to read the stream</param>
        /// <returns>The root of the deserialized node tree</returns>
        private static TreeNode Deserialize(BinaryReader reader)
        {
            string name = reader.ReadString();
            if (name == string.Empty)
            {
                return null;
            }

            int dataLength = reader.ReadInt32();
            byte[] data = null;
            if (dataLength >= 0)
            {
                data = reader.ReadBytes(dataLength);
            }

            // Stat is optional.
            var stat = reader.ReadBoolean()
                ? Stat.ReadStat(reader)
                : null;

            List<TreeNode> children = null;
            while (true)
            {
                TreeNode child = TreeNode.Deserialize(reader);
                if (child == null)
                {
                    break;
                }

                if (children == null)
                {
                    children = new List<TreeNode>();
                }

                children.Add(child);
            }

            return new TreeNode(name, data, stat, children);
        }
    }
}
