// <copyright file="Callbacks.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

#pragma warning disable SA1201, SA1649 // delegate should now follow class

    /// <summary>
    /// Delegate StringCallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="path">The path.</param>
    /// <param name="ctx">The CTX.</param>
    /// <param name="name">The name.</param>
    public delegate void StringCallbackDelegate(int rc, string path, object ctx, string name);

    /// <summary>
    /// Interface IStringCallback
    /// </summary>
    public interface IStringCallback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="name">The name.</param>
        void ProcessResult(int rc, string path, object ctx, string name);
    }

    /// <summary>
    /// Delegate OpResultCallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="res">The list of opresults.</param>
    /// <param name="ctx">The CTX.</param>
    public delegate void OpsResultCallbackDelegate(int rc, IReadOnlyList<OpResult> res, object ctx);

    /// <summary>
    /// Interface IOpsResultCallback
    /// </summary>
    public interface IOpsResultCallback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="res">The list of opresults.</param>
        /// <param name="ctx">The CTX.</param>
        void ProcessResult(int rc, IReadOnlyList<OpResult> res, object ctx);
    }

    /// <summary>
    /// Delegate ACLCallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="path">The path.</param>
    /// <param name="ctx">The CTX.</param>
    /// <param name="acl">The acl.</param>
    /// <param name="stat">The stat.</param>
    public delegate void AclCallbackDelegate(int rc, string path, object ctx, IReadOnlyList<Acl> acl, IStat stat);

    /// <summary>
    /// Interface IACLCallback
    /// </summary>
    public interface IAclCallback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="acl">The acl.</param>
        /// <param name="stat">The stat.</param>
        void ProcessResult(int rc, string path, object ctx, IReadOnlyList<Acl> acl, IStat stat);
    }

    /// <summary>
    /// Delegate ChildrenCallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="path">The path.</param>
    /// <param name="ctx">The CTX.</param>
    /// <param name="children">The children.</param>
    public delegate void ChildrenCallbackDelegate(int rc, string path, object ctx, IReadOnlyList<string> children);

    /// <summary>
    /// Interface IChildrenCallback
    /// </summary>
    public interface IChildrenCallback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="children">The children.</param>
        void ProcessResult(int rc, string path, object ctx, IReadOnlyList<string> children);
    }

    /// <summary>
    /// Delegate Children2CallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="path">The path.</param>
    /// <param name="ctx">The CTX.</param>
    /// <param name="children">The children.</param>
    /// <param name="stat">The stat.</param>
    public delegate void Children2CallbackDelegate(int rc, string path, object ctx, IReadOnlyList<string> children, IStat stat);

    /// <summary>
    /// Interface IChildren2Callback
    /// </summary>
    public interface IChildren2Callback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="children">The children.</param>
        /// <param name="stat">The stat.</param>
        void ProcessResult(int rc, string path, object ctx, IReadOnlyList<string> children, IStat stat);
    }

    /// <summary>
    /// Delegate DataCallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="path">The path.</param>
    /// <param name="ctx">The CTX.</param>
    /// <param name="data">The data.</param>
    /// <param name="stat">The stat.</param>
    public delegate void DataCallbackDelegate(int rc, string path, object ctx, byte[] data, IStat stat);

    /// <summary>
    /// Interface IDataCallback
    /// </summary>
    public interface IDataCallback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="data">The data.</param>
        /// <param name="stat">The stat.</param>
        void ProcessResult(int rc, string path, object ctx, byte[] data, IStat stat);
    }

    /// <summary>
    /// Delegate StatCallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="path">The path.</param>
    /// <param name="ctx">The CTX.</param>
    /// <param name="stat">The stat.</param>
    public delegate void StatCallbackDelegate(int rc, string path, object ctx, IStat stat);

    /// <summary>
    /// Interface IStatCallback
    /// </summary>
    public interface IStatCallback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="stat">The stat.</param>
        void ProcessResult(int rc, string path, object ctx, IStat stat);
    }

    /// <summary>
    /// Delegate VoidCallbackDelegate
    /// </summary>
    /// <param name="rc">The rc.</param>
    /// <param name="path">The path.</param>
    /// <param name="ctx">The CTX.</param>
    public delegate void VoidCallbackDelegate(int rc, string path, object ctx);

    /// <summary>
    /// Interface IVoidCallback
    /// </summary>
    public interface IVoidCallback
    {
        /// <summary>
        /// Processes the result.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <param name="path">The path.</param>
        /// <param name="ctx">The CTX.</param>
        void ProcessResult(int rc, string path, object ctx);
    }
}
