// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="LockDownSet.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class LockDownSet will encapsulate the management of lock down paths in a thread safe manner
    /// </summary>
    public class LockDownSet
    {
        /// <summary>
        /// The current list of paths
        /// </summary>
        private HashSet<string> paths = null;

        /// <summary>
        /// Gets a value indicating whether this instance must ignore all paths and act as if none is set.
        /// </summary>
        /// <value><c>true</c> if this instance must ignore all paths and act as if none is set; otherwise, <c>false</c>.</value>
        public bool IgnoreAllpaths
        {
            get;
            internal set;
        }

        /// <summary>
        /// Determines whether the specified path is locked down.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns><c>true</c> the specified path is locked down; otherwise, <c>false</c>.</returns>
        public bool Contains(string path)
        {
            HashSet<string> result = this.paths;

            if (IgnoreAllpaths)
            {
                return false;
            }

            if (result == null || result.Count == 0)
            {
                return false;
            }

            return result.Contains(path);
        }

        /// <summary>
        /// Replaces the set of locked down paths with the given one
        /// </summary>
        /// <param name="newPaths">The new set of paths.</param>
        public void ReplacePaths(HashSet<string> newPaths)
        {
            this.paths = newPaths;
        }

        /// <summary>
        /// Gets all the paths in the set.
        /// </summary>
        /// <returns>the enumeration with all the paths</returns>
        public IEnumerable<string> GetPaths()
        {
            HashSet<string> result = this.paths;

            if (IgnoreAllpaths)
            {
                return new string[0];
            }

            if (result != null)
            {
                return result;
            }
            else
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Determines whether this instance has no path.
        /// </summary>
        /// <returns><c>true</c> if this instance is empty; otherwise, <c>false</c>.</returns>
        internal bool IsEmpty()
        {
            HashSet<string> result = this.paths;
            if (IgnoreAllpaths)
            {
                return true;
            }
            return (result == null || result.Count == 0);
        }
    }
}