// <copyright file="DataEncodingHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class DataEncodingHelper.
    /// </summary>
    public class DataEncodingHelper
    {
        /// <summary>
        /// Additional size overhead for storing strings in Pascal format.
        /// </summary>
        public const int StringOverhead = sizeof(int);

        /// <summary>
        /// The sizeof stat
        /// </summary>
        public const int SizeofStat = (8 * 5) + (4 * 5);

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is 0 or less</exception>
        public static void Read(IoSession session, out byte data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 1)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            data = session.Buffer[session.Pos];
            session.Pos++;
            session.MaxBytes--;
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">if set to <c>true</c> [data].</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is 0 or less</exception>
        public static void Read(IoSession session, out bool data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 1)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            data = session.Buffer[session.Pos] != 0;

            session.Pos++;
            session.MaxBytes--;
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than the data array length</exception>
        public static void Read(IoSession session, byte[] data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (session.MaxBytes < data.Length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            Array.Copy(session.Buffer, session.Pos, data, 0, data.Length);
            session.Pos += data.Length;
            session.MaxBytes -= data.Length;
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than the data array length</exception>
        public static void Read(IoSession session, ushort[] data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int length = data.Length * sizeof(ushort);

            if (session.MaxBytes < length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            for (int pos = 0; pos < data.Length; pos++)
            {
                Read(session, out data[pos]);
            }
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than the data array length</exception>
        public static void Read(IoSession session, int[] data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int length = data.Length * sizeof(int);

            if (session.MaxBytes < length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            for (int pos = 0; pos < data.Length; pos++)
            {
                Read(session, out data[pos]);
            }
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than the data array length</exception>
        public static void Read(IoSession session, long[] data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int length = data.Length * sizeof(long);

            if (session.MaxBytes < length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            for (int pos = 0; pos < data.Length; pos++)
            {
                Read(session, out data[pos]);
            }
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 2</exception>
        public static void Read(IoSession session, out ushort data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            byte[] buffer = session.Buffer;
            int pos = session.Pos;

            if (session.MaxBytes < 2)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            data = (ushort)((int)buffer[pos + 1] | (int)buffer[pos] << 8);
            session.Pos += 2;
            session.MaxBytes -= 2;
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 4</exception>
        public static void Read(IoSession session, out uint data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            byte[] buffer = session.Buffer;
            int pos = session.Pos;
            if (session.MaxBytes < 4)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            data = (uint)((int)buffer[pos + 3] | (int)buffer[pos + 2] << 8 | (int)buffer[pos + 1] << 16 | (int)buffer[pos] << 24);
            session.Pos += 4;
            session.MaxBytes -= 4;
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 4</exception>
        public static void Read(IoSession session, out int data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            byte[] buffer = session.Buffer;
            int pos = session.Pos;

            if (session.MaxBytes < 4)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            data = (int)buffer[pos + 3] | (int)buffer[pos + 2] << 8 | (int)buffer[pos + 1] << 16 | (int)buffer[pos] << 24;
            session.Pos += 4;
            session.MaxBytes -= 4;
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 8</exception>
        public static void Read(IoSession session, out ulong data)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            byte[] buffer = session.Buffer;
            int pos = session.Pos;

            if (session.MaxBytes < 8)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            uint num = (uint)((int)buffer[pos + 7] | (int)buffer[pos + 6] << 8 | (int)buffer[pos + 5] << 16 | (int)buffer[pos + 4] << 24);
            uint num2 = (uint)((int)buffer[pos + 3] | (int)buffer[pos + 2] << 8 | (int)buffer[pos + 1] << 16 | (int)buffer[pos] << 24);
            data = (ulong)num2 << 32 | (ulong)num;

            session.Pos += 8;
            session.MaxBytes -= 8;
        }

        /// <summary>
        /// Reads the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="data">The data.</param>
        public static void Read(IoSession session, out long data)
        {
            ulong d;
            Read(session, out d);
            data = (long)d;
        }

        /// <summary>
        /// Reads a string.
        /// </summary>
        /// <param name="s">The IO session.</param>
        /// <param name="enc">The expected string encoding.</param>
        /// <param name="p">Out parameter; the read string.</param>
        public static void Read(IoSession s, Encoding enc, out string p)
        {
            if (enc == null)
            {
                throw new ArgumentNullException(nameof(enc));
            }

            int nbytes;
            Read(s, out nbytes);
            if (nbytes == -1)
            {
                p = null;
            }
            else
            {
                byte[] bytes = new byte[nbytes];
                Read(s, bytes);
                p = enc.GetString(bytes);
            }
        }

        /// <summary>
        /// Reads a <see cref="IStat"/> instance.
        /// </summary>
        /// <param name="s">The IO session.</param>
        /// <param name="stat">Out parameter; the read <see cref="IStat"/> instance.</param>
        public static void Read(IoSession s, out IStat stat)
        {
            int theint;
            long thelong;
            Stat tmpStat = new Stat();
            stat = tmpStat;
            Read(s, out thelong);
            tmpStat.Czxid = thelong;

            Read(s, out thelong);
            tmpStat.Mzxid = thelong;

            Read(s, out thelong);
            tmpStat.Pzxid = thelong;

            Read(s, out theint);
            tmpStat.Version = theint;

            Read(s, out theint);
            tmpStat.Aversion = theint;

            Read(s, out theint);
            tmpStat.Cversion = theint;

            Read(s, out thelong);
            tmpStat.Ctime = thelong;

            Read(s, out thelong);
            tmpStat.Mtime = thelong;

            Read(s, out theint);
            tmpStat.DataLength = theint;

            Read(s, out theint);
            tmpStat.NumChildren = theint;
        }

        /// <summary>
        /// Reads an <see cref="Id"/>.
        /// </summary>
        /// <param name="s">The IO session.</param>
        /// <param name="encoding">The string encoding.</param>
        /// <param name="actorId">Out parameter; the read Id.</param>
        public static void Read(IoSession s, Encoding encoding, out Id actorId)
        {
            string scheme;
            Read(s, encoding, out scheme);
            string id;
            Read(s, encoding, out id);
            actorId = new Id(scheme, id);
        }

        /// <summary>
        /// Reads an <see cref="Acl"/>.
        /// </summary>
        /// <param name="s">The IO session.</param>
        /// <param name="encoding">The string encoding.</param>
        /// <param name="acl">Out parameter; the read ACL.</param>
        public static void Read(IoSession s, Encoding encoding, out Acl acl)
        {
            int perms;
            Read(s, out perms);
            Id id;
            Read(s, encoding, out id);
            acl = new Acl(perms, id);
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than data array length</exception>
        public static void Write(byte[] data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (session.MaxBytes < data.Length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            Array.Copy(data, 0, session.Buffer, session.Pos, data.Length);

            session.Pos += data.Length;
            session.MaxBytes -= data.Length;
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than data array length</exception>
        public static void Write(ushort[] data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int length = data.Length * sizeof(ushort);

            if (session.MaxBytes < length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            foreach (ushort val in data)
            {
                Write(val, session);
            }
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than data array length</exception>
        public static void Write(int[] data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int length = data.Length * sizeof(int);

            if (session.MaxBytes < length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            foreach (int val in data)
            {
                Write(val, session);
            }
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than data array length</exception>
        public static void Write(long[] data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int length = data.Length * sizeof(long);

            if (session.MaxBytes < length)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            foreach (long val in data)
            {
                Write(val, session);
            }
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="enc">The enc.</param>
        /// <param name="session">The session.</param>
        public static void Write(string data, Encoding enc, IoSession session)
        {
            if (enc == null)
            {
                throw new ArgumentNullException(nameof(enc));
            }

            if (data == null)
            {
                Write((int)-1, session);
                return;
            }

            byte[] bytes = enc.GetBytes(data);
            Write((int)bytes.Length, session);
            Write(bytes, session);
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="enc">The enc.</param>
        /// <param name="ms">The ms.</param>
        public static void Write(string data, Encoding enc, MemoryStream ms)
        {
            if (enc == null)
            {
                throw new ArgumentNullException(nameof(enc));
            }

            if (data == null)
            {
                Write((int)-1, ms);
                return;
            }

            byte[] bytes = enc.GetBytes(data);
            Write((int)bytes.Length, ms);
            ms.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 1</exception>
        public static void Write(byte data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 1)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            session.Buffer[session.Pos] = data;

            session.Pos += 1;
            session.MaxBytes -= 1;
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">if set to <c>true</c> [data].</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 1</exception>
        public static void Write(bool data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 1)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            session.Buffer[session.Pos] = (byte)(data ? 1 : 0);

            session.Pos += 1;
            session.MaxBytes -= 1;
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 2</exception>
        public static void Write(ushort data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 2)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            session.Buffer[session.Pos + 1] = (byte)data;
            session.Buffer[session.Pos] = (byte)(data >> 8);
            session.Pos += 2;
            session.MaxBytes -= 2;
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 4</exception>
        public static void Write(int data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 4)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            session.Buffer[session.Pos + 3] = (byte)data;
            session.Buffer[session.Pos + 2] = (byte)(data >> 8);
            session.Buffer[session.Pos + 1] = (byte)(data >> 16);
            session.Buffer[session.Pos] = (byte)(data >> 24);

            session.Pos += 4;
            session.MaxBytes -= 4;
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="ms">The ms.</param>
        public static void Write(int data, MemoryStream ms)
        {
            if (ms == null)
            {
                throw new ArgumentNullException(nameof(ms));
            }

            byte[] bytes = new byte[4];

            bytes[3] = (byte)data;
            bytes[2] = (byte)(data >> 8);
            bytes[1] = (byte)(data >> 16);
            bytes[0] = (byte)(data >> 24);

            ms.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes the specified stat.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="session">The session.</param>
        public static void Write(IStat stat, IoSession session)
        {
            if (stat == null)
            {
                throw new ArgumentNullException(nameof(stat));
            }

            Write(stat.Czxid, session);
            Write(stat.Mzxid, session);
            Write(stat.Pzxid, session);
            Write(stat.Version, session);
            Write(stat.Aversion, session);
            Write(stat.Cversion, session);
            Write(stat.Ctime, session);
            Write(stat.Mtime, session);
            Write(stat.DataLength, session);
            Write(stat.NumChildren, session);
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 4</exception>
        public static void Write(uint data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 4)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            session.Buffer[session.Pos + 3] = (byte)data;
            session.Buffer[session.Pos + 2] = (byte)(data >> 8);
            session.Buffer[session.Pos + 1] = (byte)(data >> 16);
            session.Buffer[session.Pos] = (byte)(data >> 24);

            session.Pos += 4;
            session.MaxBytes -= 4;
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        public static void Write(long data, IoSession session)
        {
            Write((ulong)data, session);
        }

        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="session">The session.</param>
        /// <exception cref="IoSessionIndexOutOfRangeException">If MaxBytes is less than 8</exception>
        public static void Write(ulong data, IoSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.MaxBytes < 8)
            {
                throw new IoSessionIndexOutOfRangeException();
            }

            session.Buffer[session.Pos + 7] = (byte)data;
            session.Buffer[session.Pos + 6] = (byte)(data >> 8);
            session.Buffer[session.Pos + 5] = (byte)(data >> 16);
            session.Buffer[session.Pos + 4] = (byte)(data >> 24);
            session.Buffer[session.Pos + 3] = (byte)(data >> 32);
            session.Buffer[session.Pos + 2] = (byte)(data >> 40);
            session.Buffer[session.Pos + 1] = (byte)(data >> 48);
            session.Buffer[session.Pos] = (byte)(data >> 56);

            session.Pos += 8;
            session.MaxBytes -= 8;
        }

        /// <summary>
        /// Writes a <see cref="Id"/> instance.
        /// </summary>
        /// <param name="id">The Id to write.</param>
        /// <param name="encoding">The string encoding.</param>
        /// <param name="session">The IO session.</param>
        public static void Write(Id id, Encoding encoding, IoSession session)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Write(id.Scheme, encoding, session);
            Write(id.Identifier, encoding, session);
        }

        /// <summary>
        /// Writes a <see cref="Acl"/> instance.
        /// </summary>
        /// <param name="acl">The ACL to write.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="session">The IO session.</param>
        public static void Write(Acl acl, Encoding encoding, IoSession session)
        {
            if (acl == null)
            {
                throw new ArgumentNullException(nameof(acl));
            }

            Write(acl.Perms, session);
            Write(acl.Id, encoding, session);
        }

        /// <summary>
        /// Class IoSessionIndexOutOfRangeException abstracts the exception where an IoSession has an index out of range
        /// </summary>
        [Serializable]
        public class IoSessionIndexOutOfRangeException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="IoSessionIndexOutOfRangeException"/> class.
            /// </summary>
            /// <param name="message">The message that describes the error.</param>
            public IoSessionIndexOutOfRangeException(string message)
                : base(message)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="IoSessionIndexOutOfRangeException"/> class.
            /// </summary>
            public IoSessionIndexOutOfRangeException()
            {
            }
        }
    }
}
