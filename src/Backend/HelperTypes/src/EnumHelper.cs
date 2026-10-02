// <copyright file="EnumHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Helper class for more efficient enum utilities.
    /// </summary>
    /// <remarks>
    /// See EnumToStringFastBenchmark for the performance differences.
    /// </remarks>
    public static class EnumHelper
    {
        /// <summary>
        /// Gets a string representation of <paramref name="enumValue"/>.
        /// </summary>
        /// <param name="enumValue">The enum value.</param>
        /// <typeparam name="TEnum">An enum type.</typeparam>
        /// <returns>The string representation of the <paramref name="enumValue"/></returns>
        public static string ToStringFast<TEnum>(this TEnum enumValue)
            where TEnum : System.Enum
        {
            return EnumTraits<TEnum>.ToStringFast(enumValue);
        }

        private static class EnumTraits<TEnum>
            where TEnum : System.Enum
        {
            private static readonly Dictionary<TEnum, string> StringValueMap = GetStringValueMap();

            internal static string ToStringFast(TEnum enumValue)
            {
                if (StringValueMap.TryGetValue(enumValue, out var result))
                {
                    return result;
                }

                // It is possible that someone calls this method with an unknown value. In this case just falling back to the default implementation.
                return enumValue.ToString();
            }

            private static Dictionary<TEnum, string> GetStringValueMap()
            {
                var result = new Dictionary<TEnum, string>();
                foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
                {
                    result[value] = value.ToString();
                }

                return result;
            }
        }
    }
}
