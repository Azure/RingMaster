// <copyright file="MethodCoverageInfoKey.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace CoverageInfoExtractor.MethodCoverageInfoModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// Key object for MethodCoverageInfo class contains signature information for the method being covered
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MethodCoverageInfoKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodCoverageInfoKey"/> class.
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="className">Class name</param>
        /// <param name="nameSpace">Namespace</param>
        public MethodCoverageInfoKey(string methodName, string className, string nameSpace)
        {
            this.NameSpace = nameSpace;
            this.MethodName = methodName;
            this.ClassName = className;
            this.Parameters = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodCoverageInfoKey"/> class.
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="className">Class name</param>
        /// <param name="nameSpace">Namespace</param>
        /// <param name="parameters">List of parameters</param>
        [JsonConstructor]
        public MethodCoverageInfoKey(string methodName, string className, string nameSpace, List<string> parameters)
            : this(methodName, className, nameSpace)
        {
            this.Parameters.AddRange(parameters);
        }

        /// <summary>
        /// Gets or sets namespace of this method, can be empty if extract from coverage file
        /// </summary>
        public string NameSpace { get; set; }

        /// <summary>
        /// Gets or sets method name
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Gets or sets declare class of this method
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Gets list of parameter in C# alias
        /// </summary>
        public List<string> Parameters { get; }

        /// <summary>
        /// Gets or sets declare interface of this method
        /// </summary>
        public string InterfaceName { get; set; }

        /// <summary>
        /// Gets or sets wrapper interface of this method
        /// </summary>
        public string WrapperInterfaceName { get; set; }

        /// <summary>
        /// Gets long class name of the method, the syntax is {namespace}{classname}
        /// </summary>
        public string CoverageInfoLongClassName
        {
            get
            {
                if (string.IsNullOrEmpty(this.NameSpace))
                {
                    return this.ClassName;
                }
                else
                {
                    return $"{this.NameSpace}{this.ClassName}";
                }
            }
        }

        /// <summary>
        /// Override Equals function, comparing name space, method name, class name, parameters, interface name
        /// </summary>
        /// <param name="obj">Another object for comparison</param>
        /// <returns>Whether these 2 objects are the same</returns>
        public override bool Equals(object obj)
        {
            MethodCoverageInfoKey other = obj as MethodCoverageInfoKey;
            if (other == null)
            {
                return false;
            }

            if (!string.Equals(this.MethodName, other.MethodName, StringComparison.Ordinal)
                || !string.Equals(this.CoverageInfoLongClassName, other.CoverageInfoLongClassName, StringComparison.Ordinal)
                || !string.Equals(this.InterfaceName, other.InterfaceName, StringComparison.Ordinal)
                || this.Parameters.Count != other.Parameters.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Parameters.Count; i++)
            {
                if (!string.Equals(this.Parameters[i], other.Parameters[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Override GetHashCode for equal operations
        /// </summary>
        /// <returns>Hashcode for the object</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1307:Specify StringComparison", Justification = "GetHashCode(StringComparison) is not supported in .net 462")]
        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = (hashCode * 23) + this.CoverageInfoLongClassName.GetHashCode();
            hashCode = (hashCode * 23) + this.MethodName.GetHashCode();
            hashCode = (hashCode * 23) + (string.IsNullOrEmpty(this.InterfaceName) ? 0 : this.InterfaceName.GetHashCode());
            foreach (var parameter in this.Parameters)
            {
                hashCode = (hashCode * 23) + parameter.GetHashCode();
            }

            return hashCode;
        }
    }
}
