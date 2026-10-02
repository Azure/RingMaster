// <copyright file="MethodCoverageInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace CoverageInfoExtractor.MethodCoverageInfoModel
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Parsed Method coverage info from coverage file
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject]
    public class MethodCoverageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodCoverageInfo"/> class.
        /// </summary>
        /// <param name="key">MethodCoverageInfoKey object</param>
        /// <param name="value">MethodCoverageInfoValue object</param>
        [JsonConstructor]
        public MethodCoverageInfo(MethodCoverageInfoKey key, MethodCoverageInfoValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the MethodCoverageInfoKey object
        /// Keep setter for JSON deserialization
        /// </summary>
        [JsonProperty]
        public MethodCoverageInfoKey Key { get; set; }

        /// <summary>
        /// Gets or sets the MethodCoverageInfoValue object
        /// Keep setter for JSON deserialization
        /// </summary>
        [JsonProperty]
        public MethodCoverageInfoValue Value { get; set; }
    }
}
