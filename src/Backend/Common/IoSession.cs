// <copyright file="IoSession.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Class IOSession.
    /// </summary>
    public class IoSession
    {
        /// <summary>
        /// The next
        /// </summary>
        private IoSession next;

        /// <summary>
        /// Enum JumpBehavior
        /// </summary>
        public enum JumpBehavior
        {
            /// <summary>
            /// The adjust position
            /// </summary>
            AdjustPosition = 1,

            /// <summary>
            /// The pop position
            /// </summary>
            PopPosition,

            /// <summary>
            /// The throw if jump needed
            /// </summary>
            ThrowIfJumpNeeded,
        }

        /// <summary>
        /// Gets or sets the buffer
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the maximum bytes
        /// </summary>
        public int MaxBytes { get; set; }

        /// <summary>
        /// Gets or sets the position
        /// </summary>
        public int Pos { get; set; }

        /// <summary>
        /// Gets or sets the cached positions
        /// </summary>
        internal Dictionary<string, ushort> CachedPositions { get; set; }

        /// <summary>
        /// Gets the cached positions.
        /// </summary>
        /// <param name="createIfNeeded">if set to <c>true</c> [create if needed].</param>
        /// <returns>Dictionary&lt;System.String, System.UInt16&gt;.</returns>
        public Dictionary<string, ushort> GetCachedPositions(bool createIfNeeded)
        {
            if (createIfNeeded && this.CachedPositions == null)
            {
                this.CachedPositions = new Dictionary<string, ushort>();
            }

            return this.CachedPositions;
        }

        /// <summary>
        /// Pushes the new length of the position and.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="maxLength">The maximum length.</param>
        internal void PushNewPositionAndLength(int position, int maxLength)
        {
            this.next = new IoSession();
            this.next.Buffer = this.Buffer;
            this.next.Pos = position;
            this.next.MaxBytes = this.MaxBytes;
            this.MaxBytes = maxLength;
        }

        /// <summary>
        /// Pushes the new length.
        /// </summary>
        /// <param name="maxLength">The maximum length.</param>
        internal void PushNewLength(ushort maxLength)
        {
            this.next = new IoSession();
            this.next.Buffer = this.Buffer;
            this.next.Pos = this.Pos;
            this.next.MaxBytes = this.MaxBytes;
            this.MaxBytes = maxLength;
        }

        /// <summary>
        /// Pops the length.
        /// </summary>
        /// <param name="jumpBeh">The jump beh.</param>
        /// <exception cref="System.InvalidOperationException">jump required after consuming metered field</exception>
        internal void PopLength(JumpBehavior jumpBeh = JumpBehavior.ThrowIfJumpNeeded)
        {
            switch (jumpBeh)
            {
                case JumpBehavior.ThrowIfJumpNeeded:
                    if (this.MaxBytes != 0)
                    {
                        throw new InvalidOperationException("jump required after consuming metered field");
                    }
                    else
                    {
                        int nextOffset = this.Pos - this.next.Pos;
                        this.MaxBytes = this.next.MaxBytes - nextOffset;
                        this.next = this.next.next;
                    }

                    break;
                case JumpBehavior.AdjustPosition:
                    {
                        int nextOffset = this.Pos - this.next.Pos;
                        this.MaxBytes = this.next.MaxBytes - nextOffset - this.MaxBytes;
                        this.next = this.next.next;
                    }

                    break;

                case JumpBehavior.PopPosition:
                    this.Pos = this.next.Pos;
                    this.MaxBytes = this.next.MaxBytes;
                    this.next = this.next.next;

                    break;
            }
        }
    }
}