// <copyright file="SecureTransportException.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception used in the secure transport
    /// </summary>
    [Serializable]
    public class SecureTransportException : Exception
    {
        private SecureTransportException(Code errorCode, string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// Error code. TODO: make it more descriptive
        /// </summary>
        public enum Code
        {
            /// <summary>
            /// Cancellation requested
            /// </summary>
            CancellationRequested = 0,

            /// <summary>
            /// Data is incomplete
            /// </summary>
            IncompleteData,

            /// <summary>
            /// Unexpected
            /// </summary>
            Unexpected,

            /// <summary>
            /// Connection failed
            /// </summary>
            ConnectionFailed,

            /// <summary>
            /// Server is already started
            /// </summary>
            AlreadyStarted,

            /// <summary>
            /// Server not started yet
            /// </summary>
            NotStarted,

            /// <summary>
            /// Timeout in validating SSL
            /// </summary>
            SslValidationTimedout,

            /// <summary>
            /// Duplicated certificates found
            /// </summary>
            DuplicateCertificates,

            /// <summary>
            /// Certificate is missing
            /// </summary>
            MissingCertificate,

            /// <summary>
            /// No server certificate is provided
            /// </summary>
            NoServerCertificate,

            /// <summary>
            /// Start timed out
            /// </summary>
            StartTimedout,

            /// <summary>
            /// Stop timed out
            /// </summary>
            StopTimedout,

            /// <summary>
            /// Send queue is full
            /// </summary>
            SendQueueFull,

            /// <summary>
            /// The accept connection timed out
            /// </summary>
            AcceptConnectionTimedout,

            /// <summary>
            /// Too many consecutive accept connection failures
            /// </summary>
            TooManyConsecutiveAcceptFailures,
        }

        /// <summary>
        /// Gets the error code of the exception
        /// </summary>
        public Code ErrorCode { get; private set; }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.CancellationRequested"/>
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <returns>Exception object</returns>
        public static Exception CancellationRequested(string message)
        {
            return new SecureTransportException(Code.CancellationRequested, message);
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.CancellationRequested"/>
        /// </summary>
        /// <param name="expectedLength">Length of expected data</param>
        /// <param name="actualLength">Length of actual data</param>
        /// <returns>Exception object</returns>
        public static Exception IncompleteData(int expectedLength, int actualLength)
        {
            return new SecureTransportException(Code.IncompleteData, $"Failed to read complete data from the connection. expected={expectedLength} actual={actualLength}");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.Unexpected"/>
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <returns>Exception object</returns>
        public static Exception Unexpected(string message)
        {
            return new SecureTransportException(Code.Unexpected, message);
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.ConnectionFailed"/>
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception ConnectionFailed()
        {
            return new SecureTransportException(Code.ConnectionFailed, "Failed to connect to any of the provided endpoints");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.AlreadyStarted"/>
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception AlreadyStarted()
        {
            return new SecureTransportException(Code.AlreadyStarted, "SecureTransport has already started");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.NotStarted"/>
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception NotStarted()
        {
            return new SecureTransportException(Code.NotStarted, "SecureTransport has not been started");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.SslValidationTimedout"/>
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception SslValidationTimedOut()
        {
            return new SecureTransportException(Code.SslValidationTimedout, "SSL Validation timed out");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.DuplicateCertificates"/>
        /// </summary>
        /// <param name="thumbprint">Thumbprint of the cert</param>
        /// <returns>Exception object</returns>
        public static Exception DuplicateCertificates(string thumbprint)
        {
            return new SecureTransportException(Code.DuplicateCertificates, $"Two or more certificates with the thumbprint {thumbprint} were found in the same path");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.MissingCertificate"/>
        /// </summary>
        /// <param name="thumbprint">Thumbprint of the cert</param>
        /// <returns>Exception object</returns>
        public static Exception MissingCertificate(string thumbprint)
        {
            return new SecureTransportException(Code.MissingCertificate, $"Certificate with thumbprint {thumbprint} was not found");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.NoServerCertificate"/>
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception NoServerCertificate()
        {
            return new SecureTransportException(Code.NoServerCertificate, "Server certificate was not found");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.StartTimedout"/>
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception StartTimedout()
        {
            return new SecureTransportException(Code.StartTimedout, "Start timed out");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.TooManyConsecutiveAcceptFailures"/>
        /// </summary>
        /// <param name="failureCounts">Count of failures</param>
        /// <returns>Exception object</returns>
        public static Exception TooManyConsecutiveAcceptFailures(int failureCounts)
        {
            return new SecureTransportException(Code.TooManyConsecutiveAcceptFailures, $"Consecutive accept failures exceeded the limit: {failureCounts}");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.StopTimedout"/>
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception StopTimedout()
        {
            return new SecureTransportException(Code.StopTimedout, "Stop timed out");
        }

        /// <summary>
        /// Returns an exception with error code of <see cref="Code.SendQueueFull"/>
        /// </summary>
        /// <param name="outgoingPacketsCount">The number of packages in the outgoing queue</param>
        /// <returns>Exception object</returns>
        public static Exception SendQueueFull(int outgoingPacketsCount)
        {
            return new SecureTransportException(Code.SendQueueFull, $"Send queue is full. OutgoingPacketsCount: {outgoingPacketsCount}");
        }

        /// <summary>
        /// Accepts the connection timed out.
        /// </summary>
        /// <returns>Exception object</returns>
        public static Exception AcceptConnectionTimedout()
        {
            return new SecureTransportException(Code.AcceptConnectionTimedout, "AcceptConnection timed out");
        }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("Code", this.ErrorCode);
        }
    }
}
