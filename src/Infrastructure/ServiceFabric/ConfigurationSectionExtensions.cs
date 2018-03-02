// <copyright file="ConfigurationSectionExtensions.cs" company="Microsoft">
//     Copyright �  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ServiceFabric
{
    using System;
    using System.Fabric.Description;

    public static class ConfigurationSectionExtensions
    {
        public static string GetStringValue(this ConfigurationSection config, string settingName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.Parameters[settingName].Value;
        }

        public static int GetIntValue(this ConfigurationSection config, string settingName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return int.Parse(config.Parameters[settingName].Value);
        }

        public static ulong GetUInt64Value(this ConfigurationSection config, string settingName)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return ulong.Parse(config.Parameters[settingName].Value);
        }

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