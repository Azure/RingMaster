// <copyright file="MethodCoverageInfoValue.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace CoverageInfoExtractor.MethodCoverageInfoModel
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Value object model for MethodCoverageInfo, containing coverage info for a method
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MethodCoverageInfoValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodCoverageInfoValue"/> class.
        /// </summary>
        /// <param name="blocksCovered"># of blocks covered</param>
        /// <param name="blocksNotCovered">$ of blocks not covered</param>
        [JsonConstructor]
        public MethodCoverageInfoValue(uint blocksCovered, uint blocksNotCovered)
        {
            this.BlocksCovered = blocksCovered;
            this.BlocksNotCovered = blocksNotCovered;
        }

        /// <summary>
        /// Gets or sets # of blocks covered in this method
        /// </summary>
        public uint BlocksCovered { get; set; }

        /// <summary>
        /// Gets or sets # of blocks that is not covered in this method
        /// </summary>
        public uint BlocksNotCovered { get; set; }

        /// <summary>
        /// Gets a value indicating whether this method is covered
        /// </summary>
        public bool Covered => this.BlocksCovered != 0;

        /// <summary>
        /// Gets a value indicating whether this method is fully covered
        /// </summary>
        public bool FullyCovered => this.BlocksNotCovered == 0;
    }
}
