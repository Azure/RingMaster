// <copyright file="ICertificateRulesInstrumentation.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// The interface that describes the instrumentation hook
    /// </summary>
    public interface ICertificateRulesInstrumentation
    {
        /// <summary>
        /// Indicates the certificate validation finished
        /// </summary>
        /// <param name="cert">certificate validated</param>
        /// <param name="succeeded">if true the validation succeeded. otherwise, false</param>
        /// <param name="reason">reason for the action</param>
        void ValidateCertificateCompleted(X509Certificate cert, bool succeeded, string reason);
    }

    /// <summary>
    /// Null instrumentation. Just trace
    /// </summary>
    internal class SslWrappingInstrumentation : ICertificateRulesInstrumentation
    {
        /// <summary>
        /// the singleton of the null instrumentation
        /// </summary>
        public static readonly ICertificateRulesInstrumentation NullInstrumentation = new SslWrappingInstrumentation();

        /// <summary>
        /// Indicates the certificate validation finished
        /// </summary>
        /// <param name="certificate">certificate validated</param>
        /// <param name="succeeded">if true, the validation succeeded. otherwise, false</param>
        /// <param name="reason">reason for the action</param>
        public void ValidateCertificateCompleted(X509Certificate certificate, bool succeeded, string reason)
        {
            if (certificate == null)
            {
                Trace.TraceWarning("ValidateCertificateCompleted: {0} but null cert (reason = {1})", succeeded ? "Success" : "Failure", reason);
                return;
            }

            if (succeeded)
            {
                Trace.TraceInformation("ValidateClientCertificate succeeded serialNumber={0}, issuer={1}, subject={2}, thumbprint={3}, reason={4}", CertAccessor.Instance.GetSerialNumberString(certificate), CertAccessor.Instance.GetIssuer(certificate), CertAccessor.Instance.GetSubject(certificate), CertAccessor.Instance.GetThumbprint(certificate), reason);
            }
            else
            {
                string message = string.Format("ValidateClientCertificate failed serialNumber={0}, issuer={1}, subject={2}, thumbprint={3}, reason={4}", CertAccessor.Instance.GetSerialNumberString(certificate), CertAccessor.Instance.GetIssuer(certificate), CertAccessor.Instance.GetSubject(certificate), CertAccessor.Instance.GetThumbprint(certificate), reason);
                Trace.TraceError(message);
            }
        }
    }
}