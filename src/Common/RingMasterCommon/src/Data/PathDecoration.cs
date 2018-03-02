// <copyright file="PathDecoration.cs" company="Microsoft">
//      Copyright (C) 2018
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;

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
        /// Gets the full content path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="withStat">Should the node stat be returned or not</param>
        /// <returns>Decorated path to retrieve the full sub-tree</returns>
        public static string GetFullContentPath(string path, bool withStat)
        {
            return string.Join(
                PathDelimiter,
                path,
                withStat ? FullSubtreeStatPostfix : FullSubtreePostfix);
        }

        /// <summary>
        /// Determines whether [is full content path] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="withStat">Should the node stat be returned or not</param>
        /// <returns><c>true</c> if the specified path is a 'full contents' path; otherwise, <c>false</c>.</returns>
        public static bool IsFullContentPath(string path, out bool withStat)
        {
            bool isFullSubTree = false;

            withStat = false;

            if (!string.IsNullOrEmpty(path) && path[path.Length - 1] == MagicChar)
            {
                var postfix = path.Substring(path.LastIndexOf(PathDelimiterChar) + 1);
                if (postfix.Equals(FullSubtreePostfix, StringComparison.Ordinal))
                {
                    isFullSubTree = true;
                }
                else if (postfix.Equals(FullSubtreeStatPostfix, StringComparison.Ordinal))
                {
                    withStat = true;
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
    }
}
