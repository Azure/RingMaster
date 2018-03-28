// <copyright file="MonitoringConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric
{
    using System.Fabric;

    /// <summary>
    /// Monitoring configuration is used by all services to connect with
    /// and identify themselves to Geneva Monitoring agent.
    /// </summary>
    public sealed class MonitoringConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringConfiguration"/> class.
        /// </summary>
        /// <param name="context">code package activation context</param>
        /// <param name="sectionName">Section name</param>
        public MonitoringConfiguration(ICodePackageActivationContext context, string sectionName = "Monitoring")
        {
            var config = context.GetConfigurationSection(sectionName);

            this.Environment = config.GetStringValue("Environment");
            this.Tenant = config.GetStringValue("Tenant");
            this.Role = config.GetStringValue("Role");
            this.RoleInstance = System.Environment.GetEnvironmentVariable("Fabric_NodeName");
            this.IfxSession = config.GetStringValue("IfxSession");
            this.MdmAccount = config.GetStringValue("MdmAccount");
            this.MdmNamespace = config.GetStringValue("MdmNamespace");
        }

        /// <summary>
        /// Gets the environment name, prod or test, etc.
        /// </summary>
        public string Environment { get; private set; }

        /// <summary>
        /// Gets the tenant name
        /// </summary>
        public string Tenant { get; private set; }

        /// <summary>
        /// Gets the role name, not in use
        /// </summary>
        public string Role { get; private set; }

        /// <summary>
        /// Gets the role instance name
        /// </summary>
        public string RoleInstance { get; private set; }

        /// <summary>
        /// Gets the IFx session name
        /// </summary>
        public string IfxSession { get; private set; }

        /// <summary>
        /// Gets the MDM account name
        /// </summary>
        public string MdmAccount { get; private set; }

        /// <summary>
        /// Gets the MDM namespace
        /// </summary>
        public string MdmNamespace { get; private set; }
    }
}