// <copyright file="ICodePackageActivationContextExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;

    /// <summary>
    /// Extension methods for <see cref="ICodePackageActivationContext"/>
    /// </summary>
    public static class ICodePackageActivationContextExtensions
    {
        /// <summary>
        /// Gets the config section
        /// </summary>
        /// <param name="context">Code package activation context</param>
        /// <param name="sectionName">Name of the section to get</param>
        /// <param name="configurationPackageName">Name of the config package</param>
        /// <returns>Config section</returns>
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