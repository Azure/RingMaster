// <copyright file="SetDataOperationHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Set data operation helper
    /// </summary>
    internal sealed class SetDataOperationHelper : ISetDataOperationHelper
    {
        private const uint Magic = 0xf00dface;
        private const int Length = sizeof(uint) + sizeof(ushort) + sizeof(long);

        /// <summary>
        /// Gets the global instance
        /// </summary>
        public static SetDataOperationHelper Instance { get; } = new SetDataOperationHelper();

        /// <summary>
        /// returns the value corresponding to the byte[] stored in the node
        /// </summary>
        /// <param name="bytes">the data as retrieved from the node</param>
        /// <returns>the long retrieved.</returns>
        public long GetValue(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            long number;

            IoSession ios = new IoSession() { Buffer = bytes, MaxBytes = bytes.Length };

            DataEncodingHelper.Read(ios, out number);

            return number;
        }

        /// <summary>
        /// creates a byte[] for SetData representing a "InterlockedAdd(value)" operation
        /// </summary>
        /// <param name="value">value to add</param>
        /// <returns>the byte[] with the encoded operation.</returns>
        public ISetDataOperation InterlockedAdd(long value)
        {
            SetDataOperationCode op = SetDataOperationCode.InterlockedAddIfVersion;

            IoSession ios = new IoSession() { Buffer = new byte[Length], MaxBytes = Length };

            DataEncodingHelper.Write(Magic, ios);
            DataEncodingHelper.Write((ushort)op, ios);
            DataEncodingHelper.Write(value, ios);
            return new SetDataOperation(ios.Buffer);
        }

        /// <summary>
        /// creates a byte[] for SetData representing a "InterlockedXOR(value)" operation
        /// </summary>
        /// <param name="value">value to compute the XOR with</param>
        /// <returns>the byte[] with the encoded operation.</returns>
        public ISetDataOperation InterlockedXOR(long value)
        {
            SetDataOperationCode op = SetDataOperationCode.InterlockedXORIfVersion;

            IoSession ios = new IoSession() { Buffer = new byte[Length], MaxBytes = Length };

            DataEncodingHelper.Write(Magic, ios);
            DataEncodingHelper.Write((ushort)op, ios);
            DataEncodingHelper.Write(value, ios);
            return new SetDataOperation(ios.Buffer);
        }

        /// <summary>
        /// creates a byte[] for SetData representing a "InterlockedSet(value)" operation
        /// </summary>
        /// <param name="value">value to set</param>
        /// <returns>the byte[] with the encoded operation.</returns>
        public ISetDataOperation InterlockedSet(long value)
        {
            IoSession ios = new IoSession() { Buffer = new byte[sizeof(long)], MaxBytes = Length };

            DataEncodingHelper.Write(value, ios);
            return new SetDataOperation(ios.Buffer);
        }

        /// <summary>
        /// tries to read the operation from the byte[] (which may or may not be a "SetDataOperation").
        /// </summary>
        /// <param name="data">byte[] with the data</param>
        /// <param name="op">operation encoded in the byte[]</param>
        /// <param name="number">the number argument of the operation</param>
        /// <returns>true if the byte[] contained a setdata operation. False otherwise</returns>
        internal bool TryRead(byte[] data, out SetDataOperationCode op, out long number)
        {
            if (data == null || data.Length != Length)
            {
                op = SetDataOperationCode.None;
                number = 0;
                return false;
            }

            IoSession ios = new IoSession() { Buffer = data, MaxBytes = data.Length };

            uint cookie;
            DataEncodingHelper.Read(ios, out cookie);
            if (cookie != Magic)
            {
                op = SetDataOperationCode.None;
                number = 0;
                return false;
            }

            ushort opshort;

            DataEncodingHelper.Read(ios, out opshort);

            op = (SetDataOperationCode)opshort;
            if (op == (int)SetDataOperationCode.None)
            {
                number = 0;
                return false;
            }

            DataEncodingHelper.Read(ios, out number);
            return true;
        }
    }
}