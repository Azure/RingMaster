// <copyright file="IStat.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;

    /// <summary>
    /// Interface to an object that provides statistics data for a node.
    /// </summary>
    public interface IStat
    {
        /// <summary>
        /// Gets the id of the transaction that created the node.
        /// </summary>
        long Czxid { get; }

        /// <summary>
        /// Gets the id of the most recent transaction that modified this node's data.
        /// </summary>
        long Mzxid { get; }

        /// <summary>
        /// Gets the id of the most recent transaction that modified this node's children.
        /// </summary>
        long Pzxid { get; }

        /// <summary>
        /// Gets the time at which this node was created.
        /// </summary>
        long Ctime { get; }

        /// <summary>
        /// Gets the time at which the last modification of this node's data was performed.
        /// </summary>
        long Mtime { get; }

        /// <summary>
        /// Gets the version of this node's data.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Gets the version number of the most recent change to this node's children.
        /// </summary>
        int Cversion { get; }

        /// <summary>
        /// Gets the version number of the most recent change to this node's <see cref="Acl"/>.
        /// </summary>
        int Aversion { get; }

        /// <summary>
        /// Gets the length of the data associated with this node.
        /// </summary>
        int DataLength { get; }

        /// <summary>
        /// Gets the number of children of this node.
        /// </summary>
        int NumChildren { get; }

        /// <summary>
        /// Gets the unique incarnation id for this object
        /// </summary>
        Guid UniqueIncarnationId { get; }

        /// <summary>
        /// Gets the unique incarnation id for this object, also considering changes on its children.
        /// </summary>
        Guid UniqueExtendedIncarnationId { get; }
    }
}