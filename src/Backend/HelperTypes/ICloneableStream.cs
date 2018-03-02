// <copyright file="ICloneableStream.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System.IO;

    /// <summary>
    /// A stream that can be cloned
    /// </summary>
    public interface ICloneableStream
    {
        /// <summary>
        /// Clones the stream.
        /// </summary>
        /// <returns>cloned Stream</returns>
        Stream CloneStream();
    }
}