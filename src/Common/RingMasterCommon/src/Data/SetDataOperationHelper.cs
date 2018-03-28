// <copyright file="SetDataOperationHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;
    using System.Net;

    /// <summary>
    /// Helper class to construct <see cref="ISetDataOperation"/>s.
    /// </summary>
    public sealed class SetDataOperationHelper : ISetDataOperationHelper
    {
        /// <summary>
        /// A magic number
        /// </summary>
        private const uint Magic = 0xf00dface;

        /// <summary>
        /// The size of a set data operation
        /// </summary>
        private const int Length = sizeof(uint) + sizeof(ushort) + sizeof(long);

        /// <summary>
        /// Magic number represented as bytes in wire order.
        /// </summary>
        private static readonly byte[] MagicBytes = GetBytes(Magic);

        /// <summary>
        /// InterlockedAddIfVersion code represented as bytes in wire order.
        /// </summary>
        private static readonly byte[] InterlockedAddIfVersionBytes = GetBytes((ushort)SetDataOperationCode.InterlockedAddIfVersion);

        /// <summary>
        /// InterlockedXORIfVersion code represented as bytes in wire order.
        /// </summary>
        private static readonly byte[] InterlockedXORIfVersionBytes = GetBytes((ushort)SetDataOperationCode.InterlockedXORIfVersion);

        /// <summary>
        /// Initializes static members of the <see cref="SetDataOperationHelper" /> class.
        /// </summary>
        static SetDataOperationHelper()
        {
            Instance = new SetDataOperationHelper();
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="SetDataOperationHelper"/> class from being created.
        /// </summary>
        private SetDataOperationHelper()
        {
        }

        /// <summary>
        /// The set of possible set data operation codes
        /// </summary>
        private enum SetDataOperationCode : ushort
        {
            /// <summary>
            /// No operation
            /// </summary>
            None = 0,

            /// <summary>
            /// Add operation
            /// </summary>
            InterlockedAddIfVersion = 1,

            /// <summary>
            /// XOR operation
            /// </summary>
            InterlockedXORIfVersion = 2,
        }

        /// <summary>
        /// Gets the singleton instance of SetDataOperationHelper.
        /// </summary>
        public static ISetDataOperationHelper Instance { get; private set; }

        /// <summary>
        /// Retrieves the operand if the given dataCommand is an InterlockedXOR data command.
        /// </summary>
        /// <param name="dataCommand">A byte array that contains an encoded an InterlockedXOR data command</param>
        /// <param name="operand">Operand for the XOR operation</param>
        /// <returns><c>true</c> if the given operation could be decoded as an InterlockedXOR operation</returns>
        public static bool TryGetInterlockedXOROperand(byte[] dataCommand, out long operand)
        {
            SetDataOperationCode code;
            if (TryDecodeOperation(dataCommand, out code, out operand))
            {
                return code == SetDataOperationCode.InterlockedXORIfVersion;
            }

            return false;
        }

        /// <summary>
        /// Returns the value encoded in the given data.
        /// </summary>
        /// <param name="bytes">Data to decode</param>
        /// <returns>Encoded value</returns>
        public long GetValue(byte[] bytes)
        {
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>
        /// Create a <see cref="ISetDataOperation"/> that represents a <c>InterlockedAdd(value)</c> operation.
        /// </summary>
        /// <param name="value">Value to <c>ADD</c> with the data already in the node</param>
        /// <returns>A <see cref="ISetDataOperation"/> that represents the encoded operation as a data command</returns>
        public ISetDataOperation InterlockedAdd(long value)
        {
            byte[] buffer = new byte[SetDataOperationHelper.Length];
            MagicBytes.CopyTo(buffer, 0);
            InterlockedAddIfVersionBytes.CopyTo(buffer, MagicBytes.Length);
            GetBytes(value).CopyTo(buffer, MagicBytes.Length + InterlockedAddIfVersionBytes.Length);

            return new SetDataOperation(buffer);
        }

        /// <summary>
        /// Create a <see cref="ISetDataOperation"/> that represents a <c>InterlockedXor(value)</c> operation.
        /// </summary>
        /// <param name="value">Value to <c>XOR</c> with the data already in the node</param>
        /// <returns>A <see cref="ISetDataOperation"/> that represents the encoded operation as a data command</returns>
        public ISetDataOperation InterlockedXOR(long value)
        {
            byte[] buffer = new byte[SetDataOperationHelper.Length];
            MagicBytes.CopyTo(buffer, 0);
            InterlockedXORIfVersionBytes.CopyTo(buffer, MagicBytes.Length);
            GetBytes(value).CopyTo(buffer, MagicBytes.Length + InterlockedAddIfVersionBytes.Length);

            return new SetDataOperation(buffer);
        }

        /// <summary>
        /// Create a <see cref="ISetDataOperation"/> that represents a <c>InterlockedSet(value)</c> operation.
        /// </summary>
        /// <param name="value">value to set</param>
        /// <returns>A <see cref="ISetDataOperation"/> that represents the encoded operation as a data command</returns>
        public ISetDataOperation InterlockedSet(long value)
        {
            return new SetDataOperation(GetBytes(value));
        }

        /// <summary>
        /// Gets the set data operation bytes for a given value.
        /// </summary>
        /// <param name="value">Value to get the bytes for.</param>
        /// <returns>Set data operation bytes for the value.</returns>
        private static byte[] GetBytes(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Gets the set data operation bytes for a given value.
        /// </summary>
        /// <param name="value">Value to get the bytes for.</param>
        /// <returns>Set data operation bytes for the value.</returns>
        private static byte[] GetBytes(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Gets the set data operation bytes for a given value.
        /// </summary>
        /// <param name="value">Value to get the bytes for.</param>
        /// <returns>Set data operation bytes for the value.</returns>
        private static byte[] GetBytes(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Tries to decode the given byte array which is encoded as a data command
        /// </summary>
        /// <param name="data">Byte array that encodes a data command</param>
        /// <param name="code">The operation code of the operation that is encoded</param>
        /// <param name="number">The operand for the operation</param>
        /// <returns><c>true</c> if the given byte array contained a data command that could be decoded</returns>
        private static bool TryDecodeOperation(byte[] data, out SetDataOperationCode code, out long number)
        {
            if (data == null || data.Length != SetDataOperationHelper.Length)
            {
                code = SetDataOperationCode.None;
                number = 0;
                return false;
            }

            uint magic = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 0));
            if (magic != SetDataOperationHelper.Magic)
            {
                code = SetDataOperationCode.None;
                number = 0;
                return false;
            }

            code = (SetDataOperationCode)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, sizeof(uint)));
            if (code == SetDataOperationCode.None)
            {
                number = 0;
                return false;
            }

            number = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(data, sizeof(uint) + sizeof(ushort)));
            return true;
        }
    }
}
