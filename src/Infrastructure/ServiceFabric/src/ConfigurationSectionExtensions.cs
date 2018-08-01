// <copyright file="ConfigurationSectionExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric
{
    using System;
    using System.Fabric.Description;

    /// <summary>
    /// Extension methods for <see cref="ConfigurationSection"/> object
    /// </summary>
    public static class ConfigurationSectionExtensions
    {
        /// <summary>
        /// Gets a string setting value
        /// </summary>
        /// <param name="config">Config section</param>
        /// <param name="settingName">Name of the setting</param>
        /// <returns>Setting value in string</returns>
        public static string GetStringValue(this ConfigurationSection config, string settingName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.Parameters[settingName].Value;
        }

        /// <summary>
        /// Gets an integer setting value
        /// </summary>
        /// <param name="config">Config section</param>
        /// <param name="settingName">Name of the setting</param>
        /// <returns>Setting value in integer</returns>
        public static int GetIntValue(this ConfigurationSection config, string settingName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return int.Parse(config.Parameters[settingName].Value);
        }

        /// <summary>
        /// Gets a unsigned long integer setting value
        /// </summary>
        /// <param name="config">Config section</param>
        /// <param name="settingName">Name of the setting</param>
        /// <returns>Setting value in unsigned long</returns>
        public static ulong GetUInt64Value(this ConfigurationSection config, string settingName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return ulong.Parse(config.Parameters[settingName].Value);
        }

        /// <summary>
        /// Gets a boolean setting value
        /// </summary>
        /// <param name="config">Config section</param>
        /// <param name="settingName">Name of the setting</param>
        /// <returns>Setting value in boolean</returns>
        public static bool GetBoolValue(this ConfigurationSection config, string settingName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return bool.Parse(config.Parameters[settingName].Value);
        }
    }
}