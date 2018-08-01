// <copyright file="Op.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;
    using System.Collections.Generic;
    using RingMaster.Requests;

    /// <summary>
    /// An operation that can be part of a <see cref="RequestMulti"/> or <see cref="RequestBatch"/>.
    /// </summary>
    public sealed class Op
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Op"/> class with the given <see cref="IRingMasterRequest"/>.
        /// </summary>
        /// <param name="type">Type of the operation</param>
        /// <param name="request">The request to associate with the operation</param>
        private Op(OpCode type, IRingMasterRequest request)
        {
            this.OpType = type;
            this.Request = request;
        }

        /// <summary>
        /// Gets the type of the operation.
        /// </summary>
        public OpCode OpType { get; private set; }

        /// <summary>
        /// Gets the path of the node that this operation is associated with.
        /// </summary>
        public string Path
        {
            get
            {
                return this.Request.Path;
            }
        }

        /// <summary>
        /// Gets the <see cref="IRingMasterRequest"/> that corresponds to this operation.
        /// </summary>
        public IRingMasterRequest Request { get; private set; }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a Create operation.
        /// </summary>
        /// <param name="path">Path of the node to be created</param>
        /// <param name="data">Data to associate with the node</param>
        /// <param name="acl"><see cref="Acl"/>s to associate with the node</param>
        /// <param name="mode">Creation mode of the node</param>
        /// <returns>A Create operation</returns>
        public static Op Create(string path, byte[] data, IReadOnlyList<Acl> acl, CreateMode mode)
        {
            return new Op(OpCode.Create, new RequestCreate(path, data, acl, mode));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a Move operation.
        /// </summary>
        /// <param name="path">Path of the node to be moved</param>
        /// <param name="version">version of the node</param>
        /// <param name="pathDst">Path that will be the new parent of the moved node</param>
        /// <param name="mode">Move mode of the node</param>
        /// <returns>A Move operation</returns>
        public static Op Move(string path, int version, string pathDst, MoveMode mode)
        {
            return new Op(OpCode.Move, new RequestMove(path, version, pathDst, mode));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a Check operation.
        /// </summary>
        /// <param name="path">Path of the node to check</param>
        /// <param name="version">Version of the node's data</param>
        /// <returns>A Check operation</returns>
        public static Op Check(string path, int version)
        {
            return new Op(OpCode.Check, new RequestCheck(path, version));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a Check operation.
        /// </summary>
        /// <param name="path">Path of the node to check</param>
        /// <param name="version">Version of the node's data</param>
        /// <param name="cversion">Children version</param>
        /// <param name="aversion">ACL version</param>
        /// <returns>A Check operation</returns>
        public static Op Check(string path, int version, int cversion, int aversion)
        {
            return new Op(
                OpCode.Check,
                new RequestCheck(
                    path: path,
                    version: version,
                    cversion: cversion,
                    aversion: aversion,
                    uniqueIncarnation: Guid.Empty,
                    uniqueIncarnationIdKind: RequestCheck.UniqueIncarnationIdType.None));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a Check operation.
        /// </summary>
        /// <param name="path">Path of the node to check</param>
        /// <param name="uniqueIncarnationId">Unique incarnation id of the node</param>
        /// <param name="isExtended">If <c>true</c>, extended unique incarnation id must be used</param>
        /// <returns>A Check operation</returns>
        public static Op Check(string path, Guid uniqueIncarnationId, bool isExtended = false)
        {
            return new Op(
                OpCode.Check,
                new RequestCheck(
                    path: path,
                    version: -1,
                    cversion: -1,
                    aversion: -1,
                    uniqueIncarnation: uniqueIncarnationId,
                    uniqueIncarnationIdKind: isExtended ? RequestCheck.UniqueIncarnationIdType.Extended : RequestCheck.UniqueIncarnationIdType.Simple));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a GetData operation.
        /// </summary>
        /// <param name="path">Path of the node whose data must be retrieved</param>
        /// <param name="options">Options that specify how the data must be retrieved</param>
        /// <param name="checkUsedForThisPath">Check operation that was used for this path</param>
        /// <returns>A GetData operation</returns>
        public static Op GetData(string path, RequestGetData.GetDataOptions options, Op checkUsedForThisPath)
        {
            return GetData(path, options, null, checkUsedForThisPath);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a GetData operation.
        /// </summary>
        /// <param name="path">Path of the node whose data must be retrieved</param>
        /// <param name="options">Options that specify how the data must be retrieved</param>
        /// <param name="optionsArgument">Argument for options</param>
        /// <param name="checkUsedForThisPath">Check operation that was used for this path</param>
        /// <returns>A GetData operation</returns>
        public static Op GetData(string path, RequestGetData.GetDataOptions options, RequestGetData.IGetDataOptionArgument optionsArgument, Op checkUsedForThisPath)
        {
            if (checkUsedForThisPath != null)
            {
                if (checkUsedForThisPath.OpType != OpCode.Check || !string.Equals(checkUsedForThisPath.Path, path))
                {
                    throw new ArgumentException("checkUsedForThisPath must be either null or a OpCheck for the same path");
                }
            }

            return new Op(OpCode.GetData, new RequestGetData(path, options, optionsArgument, watcher: null));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a  Delete operation.
        /// </summary>
        /// <param name="path">Path of the node to be deleted</param>
        /// <param name="version">Version of the node's data</param>
        /// <param name="recursive">If <c>true</c>, the nodes children will be deleted recursively</param>
        /// <returns>A Delete operation</returns>
        public static Op Delete(string path, int version, bool recursive)
        {
            return Delete(path, version, recursive ? DeleteMode.CascadeDelete : DeleteMode.None);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a  Delete operation.
        /// </summary>
        /// <param name="path">Path of the node to be deleted</param>
        /// <param name="version">Version of the node's data</param>
        /// <param name="deletemode">the delete mode for the operation</param>
        /// <returns>A Delete operation</returns>
        public static Op Delete(string path, int version, DeleteMode deletemode = DeleteMode.None)
        {
            return new Op(OpCode.Delete, new RequestDelete(path, version, deletemode));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents the given request.
        /// </summary>
        /// <param name="request"><see cref="IRingMasterRequest"/> to encapsulate</param>
        /// <returns>An Multi operation that encapsulates the given request</returns>
        public static Op Run(IRingMasterRequest request)
        {
            return new Op(OpCode.Multi, request);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a SetData operation.
        /// </summary>
        /// <param name="path">Path to the node whose data must be modified</param>
        /// <param name="data">New data that must be associated with the node</param>
        /// <param name="version">Data must be modified only if the node's version is equal to this value</param>
        /// <returns>A SetData operation</returns>
        public static Op SetData(string path, byte[] data, int version)
        {
            return new Op(OpCode.SetData, new RequestSetData(path, data, version, dataCommand: false));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a SetData operation.
        /// </summary>
        /// <param name="path">Path to the node whose data must be modified</param>
        /// <param name="data">Data command</param>
        /// <param name="version">Data must be modified only if the node's version is equal to this value</param>
        /// <returns>A SetData operation</returns>
        public static Op SetData(string path, ISetDataOperation data, int version)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            return new Op(OpCode.SetData, new RequestSetData(path, data.RawData, version, dataCommand: true));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a SetACL operation.
        /// </summary>
        /// <param name="path">Path to the node whose <see cref="Acl"/>s must be modified</param>
        /// <param name="acl">New <see cref="Acl"/>s to be set</param>
        /// <param name="version"><see cref="Acl"/>s must be modified only if the node's Aversion is equal to this value</param>
        /// <returns>A SetACL operation</returns>
        public static Op SetAcl(string path, IReadOnlyList<Acl> acl, int version)
        {
            return new Op(OpCode.SetACL, new RequestSetAcl(path, acl, version));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a GetChildren operation.
        /// </summary>
        /// <param name="path">Path to the node whose children to get</param>
        /// <param name="retrievalCondition">Retrieval condition for the GetChildren operation</param>
        /// <returns>A GetChildren operation</returns>
        public static Op GetChildren(string path, string retrievalCondition = null)
        {
            return new Op(OpCode.GetChildren, new RequestGetChildren(path, null, retrievalCondition));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents an Exists operation.
        /// </summary>
        /// <param name="path">Path to the node to get stats for.</param>
        /// <returns>An Exists operation.</returns>
        public static Op Exists(string path)
        {
            return new Op(OpCode.Exists, new RequestExists(path, null));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a Sync operation.
        /// </summary>
        /// <param name="path">Path to sync.</param>
        /// <returns>A Sync operation.</returns>
        public static Op Sync(string path)
        {
            return new Op(OpCode.Sync, new RequestSync(path));
        }

        /// <summary>
        /// Create a new instance of the <see cref="Op"/> class that represents a GetSubtree operation.
        /// </summary>
        /// <param name="path">Path to get the subtree of.</param>
        /// <param name="retrievalCondition">Request retreival condition.</param>
        /// <param name="options">Request options.</param>
        /// <returns>A GetSubtree operation.</returns>
        public static Op GetSubtree(string path, string retrievalCondition, RequestGetSubtree.GetSubtreeOptions options)
        {
            return new Op(OpCode.GetSubtree, new RequestGetSubtree(path, retrievalCondition, options));
        }
    }
}