// <copyright file="BinaryWriterExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.IO;

    /// <summary>
    /// Extensions to the <see cref="BinaryWriter"/> class.
    /// </summary>
    internal static class BinaryWriterExtensions
    {
        /// <summary>
        /// Write a string that could be null to the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="binaryWriter">Binary writer</param>
        /// <param name="stringValue">String to write to the binary writer.</param>
        public static void WriteNullableString(this BinaryWriter binaryWriter, string stringValue)
        {
            binaryWriter.Write((bool)(stringValue == null));
            if (stringValue != null)
            {
                binaryWriter.Write(stringValue);
            }
        }

        /// <summary>
        /// Write a <see cref="Guid"/> to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="binaryWriter">Binary writer</param>
        /// <param name="guid"><see cref="Guid"/> to write</param>
        public static void Write(this BinaryWriter binaryWriter, Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            binaryWriter.Write(bytes, 0, bytes.Length);
        }
    }
}