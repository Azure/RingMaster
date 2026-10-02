// <copyright file="StandardMemoryStreamFactory.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System.IO;

    /// <summary>
    /// Standard memory stream factory.
    /// </summary>
    public class StandardMemoryStreamFactory : IMemoryStreamFactory
    {
        /// <inheritdoc />
        public MemoryStream CreateStream(string tag)
        {
            return new MemoryStream();
        }

        /// <inheritdoc />
        public MemoryStream CreateStream(string tag, int requestedSize)
        {
            return new MemoryStream(requestedSize);
        }
    }
}
