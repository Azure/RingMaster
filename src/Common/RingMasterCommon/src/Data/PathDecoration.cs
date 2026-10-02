// <copyright file="PathDecoration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Collection of helper methods to handle bulk operations, get full sub-tree, etc.
    /// </summary>
    public static class PathDecoration
    {
        /// <summary>
        /// Delimiter of ring master path
        /// </summary>
        private const string PathDelimiter = "/";

        /// <summary>
        /// Delimiter of ring master path, single character
        /// </summary>
        private const char PathDelimiterChar = '/';

        /// <summary>
        /// Magic char in the ring master path to indicate it may be decorated
        /// </summary>
        private const char MagicChar = '$';

        /// <summary>
        /// Postfix to indicate the path is used to retrieve the full sub-tree
        /// </summary>
        private const string FullSubtreePostfix = "$fullsubtree$";

        /// <summary>
        /// Postfix to indicate the path is used to retrieve the full sub-tree with stat
        /// </summary>
        private const string FullSubtreeStatPostfix = "$fullsubtreestat$";

        /// <summary>
        /// The full subtree user metadata postfix
        /// </summary>
        private const string FullSubtreeUserMetadataPostfix = "$fullsubtreeusermetadata$";

        /// <summary>
        /// The full subtree stat and metadata postfix
        /// </summary>
        private const string FullSubtreeStatAndMetadataPostfix = "$fullsubtreestatandmetadata$";

        private static readonly Regex ApiVersionPattern = new Regex($@"\{ApiVersion.ApiVersionString}=(\d+)\$");

        /// <summary>
        /// Gets the full content path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="option">The option.</param>
        /// <returns>
        /// Decorated path to retrieve the full sub-tree
        /// </returns>
        public static string GetFullContentPath(string path, RequestGetSubtree.GetSubtreeOptions option)
        {
            string postfix = FullSubtreePostfix;
            switch (option)
            {
                case RequestGetSubtree.GetSubtreeOptions.IncludeStats | RequestGetSubtree.GetSubtreeOptions.IncludeUserMetadata:
                    postfix = FullSubtreeStatAndMetadataPostfix;
                    break;
                case RequestGetSubtree.GetSubtreeOptions.IncludeStats:
                    postfix = FullSubtreeStatPostfix;
                    break;
                case RequestGetSubtree.GetSubtreeOptions.IncludeUserMetadata:
                    postfix = FullSubtreeUserMetadataPostfix;
                    break;

                default:
                    break;
            }

            return string.Join(
                PathDelimiter,
                path,
                postfix);
        }

        /// <summary>
        /// Determines whether [is full content path] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        ///   <c>true</c> if the specified path is a 'full contents' path; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFullContentPath(string path, out RequestGetSubtree.GetSubtreeOptions options)
        {
            bool isFullSubTree = false;
            options = RequestGetSubtree.GetSubtreeOptions.None;

            if (!string.IsNullOrEmpty(path) && path[path.Length - 1] == MagicChar)
            {
                var postfix = path.Substring(path.LastIndexOf(PathDelimiterChar) + 1);
                if (postfix.Equals(FullSubtreePostfix, StringComparison.Ordinal))
                {
                    isFullSubTree = true;
                }
                else if (postfix.Equals(FullSubtreeStatPostfix, StringComparison.Ordinal))
                {
                    options = RequestGetSubtree.GetSubtreeOptions.IncludeStats;
                    isFullSubTree = true;
                }
                else if (postfix.Equals(FullSubtreeUserMetadataPostfix, StringComparison.Ordinal))
                {
                    options = RequestGetSubtree.GetSubtreeOptions.IncludeUserMetadata;
                    isFullSubTree = true;
                }
                else if (postfix.Equals(FullSubtreeStatAndMetadataPostfix, StringComparison.Ordinal))
                {
                    options = RequestGetSubtree.GetSubtreeOptions.IncludeStats | RequestGetSubtree.GetSubtreeOptions.IncludeUserMetadata;
                    isFullSubTree = true;
                }
            }

            return isFullSubTree;
        }

        /// <summary>
        /// Gets the base path for full content path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Base path with postfix removed</returns>
        public static string GetBasePathForFullContentPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return path.Substring(0, path.LastIndexOf(PathDelimiterChar));
        }

        /// <summary>
        /// Adds the API version to path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>path with api version</returns>
        public static string AddApiVersionToPath(string path)
        {
            return string.Join(PathDelimiter, path, ApiVersion.CurrentApiVersionPostfix);
        }

        /// <summary>
        /// Gets the path without API version.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="apiVersion">The API version.</param>
        /// <returns>path without api version postfix</returns>
        public static string GetPathWithoutApiVersion(string path, out int apiVersion)
        {
            apiVersion = 0;
            if (!string.IsNullOrEmpty(path) && path[path.Length - 1] == MagicChar)
            {
                var postfix = path.Substring(path.LastIndexOf(PathDelimiterChar) + 1);
                var match = ApiVersionPattern.Match(postfix);
                if (match.Success)
                {
                    apiVersion = int.Parse(match.Groups[1].Value);
                    return path.Substring(0, path.LastIndexOf(PathDelimiterChar));
                }
            }

            return path;
        }
    }
}
