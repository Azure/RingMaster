// <copyright file="CloneableStream.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.IO;

    /// <summary>
    /// Class CloneableStream abstracts a stream that can be cloned
    /// </summary>
    public class CloneableStream : Stream, ICloneableStream
    {
        /// <summary>
        /// The function to obtain a clone
        /// </summary>
        private readonly Func<Stream> getClone;

        /// <summary>
        /// The stream instance that can be cloned
        /// </summary>
        private Stream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloneableStream"/> class.
        /// </summary>
        /// <param name="getClone">The function to clone the stream.</param>
        public CloneableStream(Func<Stream> getClone)
            : this(getClone?.Invoke(), getClone)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloneableStream"/> class.
        /// </summary>
        /// <param name="thisStream">The instance for the stream.</param>
        /// <param name="getClone">The function to clone the stream</param>
        /// <exception cref="System.ArgumentNullException">getClone</exception>
        public CloneableStream(Stream thisStream, Func<Stream> getClone)
        {
            if (thisStream == null)
            {
                throw new ArgumentNullException(nameof(thisStream));
            }

            if (getClone == null)
            {
                throw new ArgumentNullException(nameof(getClone));
            }

            this.getClone = getClone;
            this.stream = thisStream;
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value><c>true</c> if this instance can read; otherwise, <c>false</c>.</value>
        public override bool CanRead => this.stream.CanRead;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        public override bool CanSeek => this.stream.CanSeek;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value><c>true</c> if this instance can write; otherwise, <c>false</c>.</value>
        public override bool CanWrite => this.stream.CanWrite;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <value>The length.</value>
        public override long Length => this.stream.Length;

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <value>The position.</value>
        public override long Position
        {
            get
            {
                return this.stream.Position;
            }

            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Clones the stream.
        /// </summary>
        /// <returns>cloned Stream</returns>
        public Stream CloneStream()
        {
            return new CloneableStream(this.getClone(), this.getClone);
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            this.stream.Flush();
        }

        /// <summary>
        /// Reads the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns>System.Int32.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.stream.Seek(offset, origin);
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            this.stream.SetLength(value);
        }

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream. Instead of calling this method, ensure that the stream is properly disposed.
        /// </summary>
        public override void Close()
        {
            this.stream?.Close();
            this.stream = null;
            base.Close();
        }
    }
}