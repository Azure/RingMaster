// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="BulkOperation.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Class BulkOperation.
    /// </summary>
    public class BulkOperation
    {
        /// <summary>
        /// Class MiniNode.
        /// </summary>
        public class MiniNode
        {
            /// <summary>
            /// The name
            /// </summary>
            public string Name;
            /// <summary>
            /// The children
            /// </summary>
            public List<MiniNode> Children;
            /// <summary>
            /// The data
            /// </summary>
            public byte[] Data;

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
        }

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
        /// Deserializes all data.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>MiniNode.</returns>
        public static MiniNode DeserializeAllData(byte[] bytes)
        {
            using (BinaryReader br = new BinaryReader(new MemoryStream(bytes)))
            {
                return DeserializeAllData(br);
            }
        }

        /// <summary>
        /// Deserializes all data.
        /// </summary>
        /// <param name="br">The br.</param>
        /// <returns>MiniNode.</returns>
        private static MiniNode DeserializeAllData(BinaryReader br)
        {
            string name = br.ReadString();
            if (name == String.Empty)
            {
                return null;
            }
            int dataL = br.ReadInt32();
            byte[] data = null;
            if (dataL >= 0)
            {
                data = br.ReadBytes(dataL);
            }

            MiniNode node = new MiniNode(name, data);

            List<MiniNode> children = null;
            while (true)
            {
                MiniNode child = DeserializeAllData(br);
                if (child == null)
                {
                    break;
                }
                if (children == null)
                {
                    children = new List<MiniNode>();
                }
                children.Add(child);
            }
            if (children != null)
            {
                node.Children = children;
            }
            return node;
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

            ms.Write(String.Empty);
        }

        /// <summary>
        /// Determines whether [is bulk watcher] [the specified path].
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
        /// <returns>System.String.</returns>
        public static string GetBulkWatcherName(string id)
        {
            if (id == null)
            {
                return "/$bulkwatcher";
            }
            return "/$bulkwatcher/" + id;
        }
    }
}