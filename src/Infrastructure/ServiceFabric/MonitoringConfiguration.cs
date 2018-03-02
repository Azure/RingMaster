// <copyright file="MonitoringConfiguration.cs" company="Microsoft">
//     Copyright ©  2015
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

        public string Environment { get; private set; }

        public string Tenant { get; private set; }

        public string Role { get; private set; }

        public string RoleInstance { get; private set; }

        public string IfxSession { get; private set; }

        public string MdmAccount { get; private set; }

        public string MdmNamespace { get; private set; }
    }
}