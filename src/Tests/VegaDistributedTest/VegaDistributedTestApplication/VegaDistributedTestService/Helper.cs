// <copyright file="Helper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using Microsoft.Vega.Test.Helpers;
    using DistTestCommonProto;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The helper methods.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Logs the message and set job status.
        /// </summary>
        /// <param name="logger">the logger</param>
        /// <param name="jobState">State of the job.</param>
        /// <param name="message">The message.</param>
        public static void LogAndSetJobStatus(Action<string> logger, JobState jobState, string message)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (jobState == null)
            {
                throw new ArgumentNullException(nameof(jobState));
            }

            logger(message);
            jobState.Status = message;
        }

        /// <summary>
        /// Initializes the MDM.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="roleInstance">The role instance.</param>
        public static void InitializeMdm(IConfiguration appSettings, string roleInstance)
        {
            if (!bool.TryParse(appSettings["MdmEnabled"], out bool mdmEnabled))
            {
                mdmEnabled = false;
            }

            var environment = appSettings["Environment"];
            var tenant = appSettings["Tenant"];
            var mdmAccountName = appSettings["MdmAccountName"];

            MdmHelper.Initialize(environment, tenant, mdmAccountName, roleInstance, string.Empty, MdmConstants.VegaDistributedPerfIfxSession, MdmConstants.DistributedPerfMdmNamespace, mdmEnabled);
        }
    }
}
