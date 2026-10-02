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
    using Microsoft.IO;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Class BulkOperation.
    /// </summary>
    public static class BulkOperation
    {
        /// <summary>
        /// Serializes all data.
        /// </summary>
        /// <param name="ms">The ms.</param>
        /// <param name="child">The child.</param>
        /// <param name="options">The options.</param>
        /// <param name="apiVersion">The API version.</param>
        public static void SerializeAllData(MemoryStream ms, Node child, RequestDefinitions.RequestGetSubtree.GetSubtreeOptions options, int apiVersion)
        {
            SerializeAllData(child, new BinaryWriter(ms), options, apiVersion);
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
        /// <param name="ms">The ms.</param>
        /// <param name="child">Node to serialize all the data under.</param>
        /// <param name="option">The option.</param>
        /// <param name="top">Maximum number of new nodes to serialize.</param>
        /// <param name="startingPath">Continuation path to resume from.</param>
        /// <param name="apiVersion">The API version.</param>
        /// <param name="relativeResponsePath">The relative response path.</param>
        internal static void SerializeAllDataSorted(MemoryStream ms, Node child, RequestDefinitions.RequestGetSubtree.GetSubtreeOptions option, int top, Queue<string> startingPath, int apiVersion, out string relativeResponsePath)
        {
            relativeResponsePath = null;

            var responsePathStack = new Stack<string>();
            SerializeAllDataSorted(child, new BinaryWriter(ms), option, startingPath, responsePathStack, apiVersion, ref top);

            StringBuilder stringBuilder = new StringBuilder();
            while (responsePathStack.Count > 0)
            {
                stringBuilder.Append(responsePathStack.Pop());
            }

            if (stringBuilder.Length > 0)
            {
                relativeResponsePath = stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Serializes all data.
        /// </summary>
        /// <param name="child">The child node to serialize.</param>
        /// <param name="ms">The binary writer backed by memory.</param>
        /// <param name="option">The option.</param>
        /// <param name="apiVersion">The API version.</param>
        private static void SerializeAllData(Node child, BinaryWriter ms, RequestDefinitions.RequestGetSubtree.GetSubtreeOptions option, int apiVersion)
        {
            ms.Write(child.Name);
            SerializeByteArray(ms, child.Data);

            if (apiVersion >= ApiVersion.Version1)
            {
                ms.Write((byte)option);
            }
            else
            {
                // this means the request is from an old client. Should only serialize true/false to indicate include stat or not
                ms.Write(option.HasFlag(RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.IncludeStats));
            }

            if (option.HasFlag(RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.IncludeStats))
            {
                child.NodeStat.Write(ms, apiVersion);
            }

            if (option.HasFlag(RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.IncludeUserMetadata))
            {
                SerializeByteArray(ms, child.UserMetadata);
            }

            CompleteNode cn = child as CompleteNode;
            if (cn != null)
            {
                foreach (IPersistedData n in cn.ChildrenNodes)
                {
                    SerializeAllData(n.Node, ms, option, apiVersion);
                }
            }

            ms.Write(string.Empty);
        }

        /// <summary>
        /// Serializes all data in a depth-first manner sorted by node name, supporting continuations.
        /// </summary>
        /// <param name="child">Node to serialize all the data under.</param>
        /// <param name="ms">Binary writer to serialize the data to.</param>
        /// <param name="options">The options.</param>
        /// <param name="startingPath">Continuation path to resume from.</param>
        /// <param name="continuationPathBuilder">Continuation path result if we hit max number of nodes limit.</param>
        /// <param name="apiVersion">The API version.</param>
        /// <param name="maxNodes">Maximum number of new nodes to serialize.</param>
        /// <returns>
        /// True if enumeration was fully completed, false if the max nodes limit was hit.
        /// </returns>
        /// <exception cref="ArgumentException">Invalid starting path specified. Current node is {child.Name}, but starting name is {nextNodeName} - startingPath</exception>
        private static bool SerializeAllDataSorted(Node child, BinaryWriter ms, RequestDefinitions.RequestGetSubtree.GetSubtreeOptions options, Queue<string> startingPath, Stack<string> continuationPathBuilder, int apiVersion, ref int maxNodes)
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

                // not including stat or metadata for this node as it was part of previous continuation
                ms.Write((byte)RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.None);
            }
            else
            {
                maxNodes--;

                SerializeByteArray(ms, child.Data);

                ms.Write((byte)options);
                if (options.HasFlag(RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.IncludeStats))
                {
                    child.NodeStat.Write(ms, apiVersion);
                }

                if (options.HasFlag(RequestDefinitions.RequestGetSubtree.GetSubtreeOptions.IncludeUserMetadata))
                {
                    SerializeByteArray(ms, child.UserMetadata);
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
                        if (!SerializeAllDataSorted(n.Node, ms, options, startingPath, continuationPathBuilder, apiVersion, ref maxNodes))
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
                    if (!SerializeAllDataSorted(n.Node, ms, options, startingPath, continuationPathBuilder, apiVersion, ref maxNodes))
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

        private static void SerializeByteArray(BinaryWriter ms, byte[] data)
        {
            if (data == null)
            {
                ms.Write(-1);
            }
            else
            {
                ms.Write(data.Length);
                ms.Write(data);
            }
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
