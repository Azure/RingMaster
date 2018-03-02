// <copyright file="SecureTransportException.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class SecureTransportException : Exception
    {
        private SecureTransportException(Code errorCode, string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public enum Code
        {
            CancellationRequested,
            IncompleteData,
            Unexpected,
            ConnectionFailed,
            AlreadyStarted,
            NotStarted,
            SslValidationTimedout,
            DuplicateCertificates,
            MissingCertificate,
            NoServerCertificate,
            StartTimedout,
            StopTimedout,
            SendQueueFull
        }

        public Code ErrorCode { get; private set; }

        public static Exception CancellationRequested(string message)
        {
            return new SecureTransportException(Code.CancellationRequested, message);
        }

        public static Exception IncompleteData(int expectedLength, int actualLength)
        {
            return new SecureTransportException(Code.IncompleteData, string.Format("Failed to read complete data from the connection. expected={0} actual={1}", expectedLength, actualLength));
        }

        public static Exception Unexpected(string message)
        {
            return new SecureTransportException(Code.Unexpected, message);
        }

        public static Exception ConnectionFailed()
        {
            return new SecureTransportException(Code.ConnectionFailed, "Failed to connect to any of the provided endpoints");
        }

        public static Exception AlreadyStarted()
        {
            return new SecureTransportException(Code.AlreadyStarted, "SecureTransport has already started");
        }

        public static Exception NotStarted()
        {
            return new SecureTransportException(Code.NotStarted, "SecureTransport has not been started");
        }

        public static Exception SslValidationTimedOut()
        {
            return new SecureTransportException(Code.SslValidationTimedout, "SSL Validation timed out");
        }

        public static Exception DuplicateCertificates(string thumbprint)
        {
            return new SecureTransportException(Code.DuplicateCertificates, string.Format("Two or more certificates with the thumbprint {0} were found in the same path", thumbprint));
        }

        public static Exception MissingCertificate(string thumbprint)
        {
            return new SecureTransportException(Code.MissingCertificate, string.Format("Certificate with thumbprint {0} was not found", thumbprint));
        }

        public static Exception NoServerCertificate()
        {
            return new SecureTransportException(Code.NoServerCertificate, "Server certificate was not found");
        }

        public static Exception StartTimedout()
        {
            return new SecureTransportException(Code.StartTimedout, "Start timed out");
        }

        public static Exception StopTimedout()
        {
            return new SecureTransportException(Code.StopTimedout, "Stop timed out");
        }

        public static Exception SendQueueFull()
        {
            return new SecureTransportException(Code.SendQueueFull, "Send queue is full");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("Code", this.ErrorCode);
        }
    }
}