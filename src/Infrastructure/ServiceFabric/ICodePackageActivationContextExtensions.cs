// <copyright file="ICodePackageActivationContextExtensions.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;

    public static class ICodePackageActivationContextExtensions
    {
        public static ConfigurationSection GetConfigurationSection(this ICodePackageActivationContext context, string sectionName, string configurationPackageName = "Config")
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ConfigurationPackage package = context.GetConfigurationPackageObject(configurationPackageName);
            KeyedCollection<string, ConfigurationSection> configSettings = package.Settings.Sections;
            return configSettings.Contains(sectionName) ? configSettings[sectionName] : null;
        }
    }
}