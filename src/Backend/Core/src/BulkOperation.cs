// <copyright file="BulkOperation.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Class BulkOperation.
    /// </summary>
    public static class BulkOperation
    {
        /// <summary>
        /// Serializes all data.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="writeStat">if the node stat should be serialized</param>
        /// <returns>System.Byte[].</returns>
        public static byte[] SerializeAllData(Node child, bool writeStat)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                SerializeAllData(child, new BinaryWriter(ms), writeStat);
                ms.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Determines whether the specified path represents a bulk watcher
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the specified path is a 'bulkwatcher' path; otherwise, <c>false</c>.</returns>
        public static bool IsBulkWatcher(string path)
        {
            return path != null && path.StartsWith("/$bulkwatcher/");
        }

        /// <summary>
        /// Remove the bulk watcher specifier from the path (if any)
        /// </summary>
        /// <param name="path">The path to remove the specifier from</param>
        /// <param name="wasRemoved">If the removal is successful</param>
        /// <returns>The path with the bulkwatcher specifier removed</returns>
        public static string RemoveBulkWatcherSpecifier(string path, out bool wasRemoved)
        {
            const string BulkWatcherSpecifier = "bulkwatcher:";
            const int BulkWatcherSpecifierLength = 12;

            if (path != null && path.StartsWith(BulkWatcherSpecifier))
            {
                wasRemoved = true;
                return path.Substring(BulkWatcherSpecifierLength);
            }

            wasRemoved = false;
            return path;
        }

        /// <summary>
        /// Gets the name of the bulk watcher.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The path to the node where bulk watcher information is stored</returns>
        public static string GetBulkWatcherName(string id)
        {
            if (id == null)
            {
                return "/$bulkwatcher";
            }

            return "/$bulkwatcher/" + id;
        }

        /// <summary>
        /// Serializes all data in a depth-first manner sorted by node name, supporting continuations.
        /// </summary>
        /// <param name="child">Node to serialize all the data under.</param>
        /// <param name="writeStat">Whether to write stats for each node or not.</param>
        /// <param name="top">Maximum number of new nodes to serialize.</param>
        /// <param name="startingPath">Continuation path to resume from.</param>
        /// <param name="relativeResponsePath">Continuation path result if we hit max number of nodes limit.</param>
        /// <returns>Subtree data.</returns>
        internal static byte[] SerializeAllDataSorted(Node child, bool writeStat, int top, Queue<string> startingPath, out string relativeResponsePath)
        {
            relativeResponsePath = null;

            using (MemoryStream ms = new MemoryStream())
            {
                var responsePathStack = new Stack<string>();
                SerializeAllDataSorted(child, new BinaryWriter(ms), writeStat, startingPath, responsePathStack, ref top);

                StringBuilder stringBuilder = new StringBuilder();
                while (responsePathStack.Count > 0)
                {
                    stringBuilder.Append(responsePathStack.Pop());
                }

                if (stringBuilder.Length > 0)
                {
                    relativeResponsePath = stringBuilder.ToString();
                }

                ms.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Serializes all data.
        /// </summary>
        /// <param name="child">The child node to serialize.</param>
        /// <param name="ms">The binary writer backed by memory.</param>
        /// <param name="writeStat">If the node stat should be serialized along with node data</param>
        private static void SerializeAllData(Node child, BinaryWriter ms, bool writeStat)
        {
            ms.Write(child.Name);
            if (child.Data == null)
            {
                ms.Write(-1);
            }
            else
            {
                ms.Write(child.Data.Length);
                ms.Write(child.Data);
            }

            ms.Write(writeStat);
            if (writeStat)
            {
                child.NodeStat.Write(ms);
            }

            CompleteNode cn = child as CompleteNode;
            if (cn != null)
            {
                foreach (IPersistedData n in cn.ChildrenNodes)
                {
                    SerializeAllData(n.Node, ms, writeStat);
                }
            }

            ms.Write(string.Empty);
        }

        /// <summary>
        /// Serializes all data in a depth-first manner sorted by node name, supporting continuations.
        /// </summary>
        /// <param name="child">Node to serialize all the data under.</param>
        /// <param name="ms">Binary writer to serialize the data to.</param>
        /// <param name="writeStat">Whether to write stats for each node or not.</param>
        /// <param name="startingPath">Continuation path to resume from.</param>
        /// <param name="continuationPathBuilder">Continuation path result if we hit max number of nodes limit.</param>
        /// <param name="maxNodes">Maximum number of new nodes to serialize.</param>
        /// <returns>True if enumeration was fully completed, false if the max nodes limit was hit.</returns>
        private static bool SerializeAllDataSorted(Node child, BinaryWriter ms, bool writeStat, Queue<string> startingPath, Stack<string> continuationPathBuilder, ref int maxNodes)
        {
            ms.Write(child.Name);

            if (startingPath != null && startingPath.Count > 0)
            {
                var nextNodeName = startingPath.Dequeue();
                if (child.Name != nextNodeName)
                {
                    throw new ArgumentException($"Invalid starting path specified. Current node is {child.Name}, but starting name is {nextNodeName}", nameof(startingPath));
                }

                // not including data for this node as it was part of previous continuation
                ms.Write(-1);

                // not including stat for this node as it was part of previous continuation
                ms.Write(false);
            }
            else
            {
                maxNodes--;

                if (child.Data == null)
                {
                    ms.Write(-1);
                }
                else
                {
                    ms.Write(child.Data.Length);
                    ms.Write(child.Data);
                }

                ms.Write(writeStat);
                if (writeStat)
                {
                    child.NodeStat.Write(ms);
                }
            }

            if (maxNodes <= 0)
            {
                continuationPathBuilder.Push(child.Name == "/" ? "/" : string.Concat("/", child.Name));
                ms.Write(string.Empty);
                return false;
            }

            CompleteNode cn = child as CompleteNode;
            if (cn != null)
            {
                string nextNodeName = string.Empty;
                if (startingPath != null && startingPath.Count > 0)
                {
                    nextNodeName = startingPath.Peek();

                    IPersistedData n;
                    if (cn.ChildrenMapping.TryGetValue(nextNodeName, out n))
                    {
                        if (!SerializeAllDataSorted(n.Node, ms, writeStat, startingPath, continuationPathBuilder, ref maxNodes))
                        {
                            continuationPathBuilder.Push(string.Concat("/", child.Name));
                            ms.Write(string.Empty);
                            return false;
                        }
                    }
                    else
                    {
                        // this subtree was deleted so we can forget about it and continue with next node in order
                        startingPath.Clear();
                    }
                }

                var sortedChildren = cn.RetrieveChildren($">:{maxNodes}:{nextNodeName}");

                foreach (var childNodeName in sortedChildren)
                {
                    IPersistedData n = cn.ChildrenMapping[childNodeName];
                    if (!SerializeAllDataSorted(n.Node, ms, writeStat, startingPath, continuationPathBuilder, ref maxNodes))
                    {
                        continuationPathBuilder.Push(string.Concat("/", child.Name));
                        ms.Write(string.Empty);
                        return false;
                    }
                }
            }

            ms.Write(string.Empty);

            return true;
        }

        /// <summary>
        /// Class MiniNode.
        /// </summary>
        private class MiniNode
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MiniNode"/> class.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="data">The data.</param>
            public MiniNode(string name, byte[] data)
            {
                this.Name = name;
                this.Data = data;
                this.Children = null;
            }

            /// <summary>
            /// Gets or sets the node name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the children
            /// </summary>
            public List<MiniNode> Children { get; set; }

            /// <summary>
            /// Gets or sets the node data
            /// </summary>
            public byte[] Data { get; set; }
        }
    }
}
