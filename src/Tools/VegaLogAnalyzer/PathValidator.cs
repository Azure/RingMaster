// <copyright file="PathValidator.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.VegaLogAnalyzer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The tree path validator.
    /// </summary>
    public class PathValidator
    {
        private const string RegexFile = @"ValidPathRegex.txt";

        private readonly Dictionary<string, string> helperRegex = new Dictionary<string, string>
        {
            { "GuidRegex", "([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})" },
            { "TwoDigitsRegex", "[0-9]{2}" },
            { "NumberRegex", "(?:[1-9][0-9]{0,4}|0)" },
            { "Ipv4Regex", "((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)" },
            { "ClusterNameRegex", "(.*)[prd|stage](.*)[0-9]{2}" },
        };

        private List<string> validPathRegex = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PathValidator" /> class.
        /// </summary>
        public PathValidator()
        {
            string line;
            StreamReader regexFile = new StreamReader(RegexFile);
            while ((line = regexFile.ReadLine()) != null)
            {
                if (line.StartsWith("#"))
                {
                    // treat as comments, Ignore.
                    continue;
                }

                if (line.StartsWith("$"))
                {
                    line = line.Substring(1);
                    foreach (var pair in this.helperRegex)
                    {
                        var regexName = pair.Key;
                        if (line.Contains("{" + regexName + "}"))
                        {
                            line = line.Replace("{" + regexName + "}", pair.Value);
                        }
                    }
                }

                this.validPathRegex.Add(line);
            }

            regexFile.Close();
        }

        /// <summary>
        /// Determines whether [is path valid] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if [is path valid] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPathValid(string path)
        {
            for (int i = this.validPathRegex.Count - 1; i >= 0; i--)
            {
                string regex = this.validPathRegex[i];

                if (Regex.IsMatch(path, regex))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
