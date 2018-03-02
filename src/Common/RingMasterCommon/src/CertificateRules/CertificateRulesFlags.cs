// <copyright file="CertificateRulesFlags.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;

    /// <summary>
    /// The flags for the Certificate validation rules
    /// </summary>
    [Flags]
    public enum CertificateRulesFlags
    {
        /// <summary>
        /// No additional validation flag is given (i.e. no revocation is required, and no validation of trust chain is done)
        /// </summary>
        None = 0,

        /// <summary>
        /// Whether certificate revocation checking is enabled
        /// </summary>
        MustCheckCertificateRevocation = 1,

        /// <summary>
        /// Whether certificate trust chain checking is enabled
        /// </summary>
        MustCheckCertificateTrustChain = 2,
    }
}