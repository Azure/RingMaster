// <copyright file="RequestGetData.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to get the data associated with a node.
    /// </summary>
    public class RequestGetData : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="options">Options for this request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestGetData(string path, GetDataOptions options, IWatcher watcher, ulong uid = 0)
            : this(path, options, null, watcher, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="options">Options for this request</param>
        /// <param name="optionArgument">Argument for options</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="uid">Unique Id of the request</param>
        public RequestGetData(string path, GetDataOptions options, IGetDataOptionArgument optionArgument, IWatcher watcher, ulong uid = 0)
            : base(RingMasterRequestType.GetData, path, uid)
        {
            this.Watcher = watcher;
            this.Options = options;
            this.OptionArgument = optionArgument;
        }

        /// <summary>
        /// Options for get data.
        /// </summary>
        [Flags]
        public enum GetDataOptions : byte
        {
            /// <summary>
            /// No options.
            /// </summary>
            None = 0,

            /// <summary>
            /// If the node for the path does not contain data, return the data of the closest ancestor that has data.
            /// </summary>
            FaultbackOnParentData = 1, 

            /// <summary>
            /// Do not include <see cref="IStat"/> in the result.
            /// </summary>
            NoStatRequired = 2,

            /// <summary>
            /// The path in the request must be exact - no wildcards will be honored.
            /// </summary>
            NoWildcardsForPath = 4,

            /// <summary>
            /// If the node for the path does not contain data, return the data of the closest ancestor with data that matches the argument.
            /// </summary>
            FaultbackOnParentDataWithMatch = 8,
        }

        /// <summary>
        /// Represents an argument associated with a get data option.
        /// </summary>
        public interface IGetDataOptionArgument
        {
            /// <summary>
            /// Gets the option that this argument is associated with.
            /// </summary>
            GetDataOptions Option { get; }

            /// <summary>
            /// Gets a value indicating whether the given content matches the condition
            /// </summary>
            /// <param name="bytes">Content to match</param>
            /// <returns><c>true</c> if the content matches the condition</returns>
            bool Matches(byte[] bytes);
        }

        /// <summary>
        /// Gets or sets the watcher that will be set on the node.
        /// </summary>
        public IWatcher Watcher { get; set; }

        /// <summary>
        /// Gets a value indicating whether in the case the requested path not existing, this request will return the data
        /// associated with the first ancestor of the given path that exists and has non-null data on it.
        /// </summary>
        public bool FaultbackOnParentData
        {
            get
            {
                return this.IsOptionSet(GetDataOptions.FaultbackOnParentData) || this.IsOptionSet(GetDataOptions.FaultbackOnParentDataWithMatch);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result will not contain a stat.
        /// </summary>
        public bool NoStatRequired
        {
            get
            {
                return this.IsOptionSet(GetDataOptions.NoStatRequired);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the path in this request is literal, meaning the wildcards in tree should be ignored.
        /// </summary>
        public bool NoWildcardsForPath
        {
            get
            {
                return this.IsOptionSet(GetDataOptions.NoWildcardsForPath);
            }
        }

        /// <summary>
        /// Gets all options that have been specified for this request
        /// </summary>
        public GetDataOptions Options { get; private set; }

        /// <summary>
        /// Gets argument for the specified option (if any).
        /// </summary>
        public IGetDataOptionArgument OptionArgument { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>true</c> because this request does not modify any data</returns>
        public override bool IsReadOnly()
        {
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether the given option flag is set.
        /// </summary>
        /// <param name="option">Option flag</param>
        /// <returns><c>true</c> if the option flag is set</returns>
        private bool IsOptionSet(GetDataOptions option)
        {
            return (this.Options & option) == option;
        }

        /// <summary>
        /// Matches the condition that the given content is a sub array of the data associated with the node.
        /// </summary>
        public class GetDataOptionArgumentForMatch : IGetDataOptionArgument
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="GetDataOptionArgumentForMatch"/> class.
            /// </summary>
            /// <param name="bytes">Content to compare</param>
            /// <param name="position">Position in the content from which comparison must be performed</param>
            /// <param name="condition">Specifies how the comparison must be performed</param>
            public GetDataOptionArgumentForMatch(byte[] bytes, int position, Comparison condition)
            {
                this.Condition = condition;
                this.Bytes = bytes;
                this.Position = position;
            }

            /// <summary>
            /// Type of comparison that must be performed to determine match.
            /// </summary>
            public enum Comparison : byte
            {
                /// <summary>
                /// The given argument must be the same as the data in the node.
                /// </summary>
                Equals = 0, 

                /// <summary>
                /// The given argument must be different from the data in the node.
                /// </summary>
                Different = 1, 

                /// <summary>
                /// The given argument must be greater than the value of the data in the node.
                /// </summary>
                Greater = 2,

                /// <summary>
                /// The given argument must be smaller than the value of the data in the node.
                /// </summary>
                Smaller = 3
            }

            /// <summary>
            /// Gets the option that is associated with this argument.
            /// </summary>
            public GetDataOptions Option
            {
                get
                {
                    return GetDataOptions.FaultbackOnParentDataWithMatch;
                }
            }

            /// <summary>
            /// Gets the content that must be compared with the node's data.
            /// </summary>
            public byte[] Bytes { get; private set; }

            /// <summary>
            /// Gets the position in the content from which comparison must be performed.
            /// </summary>
            public int Position { get; private set; }

            /// <summary>
            /// Gets the type of comparison that must be performed.
            /// </summary>
            public Comparison Condition { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the given content matches the content in the node.
            /// </summary>
            /// <param name="bytes">Content to match with the content in the node</param>
            /// <returns><c>true</c> if the content matches</returns>
            public bool Matches(byte[] bytes)
            {
                int comp = this.CompareTo(this.Bytes, bytes, this.Position);

                if (comp == -2)
                {
                    return false;
                }

                if (this.Condition == Comparison.Different)
                {
                    return comp != 0;
                }

                if (this.Condition == Comparison.Equals)
                {
                    return comp == 0;
                }

                if (this.Condition == Comparison.Greater)
                {
                    return comp > 0;
                }

                if (this.Condition == Comparison.Smaller)
                {
                    return comp < 0;
                }

                // unknown comp
                return false;
            }

            /// <summary>
            /// Compares one byte array with another, starting from the given position.
            /// </summary>
            /// <param name="lhs">Left hand side of the comparison</param>
            /// <param name="rhs">Right hand side of the comparison</param>
            /// <param name="position">Position to start the comparison from</param>
            /// <returns>Result of the comparison</returns>
            private int CompareTo(byte[] lhs, byte[] rhs, int position)
            {
                if (lhs == rhs)
                {
                    return 0;
                }

                if (lhs == null)
                {
                    return -1;
                }

                if (rhs == null)
                {
                    return 1;
                }

                if (rhs.Length < position + lhs.Length)
                {
                    return -2;
                }

                for (int i = 0, j = position; i < lhs.Length; i++, j++)
                {
                    int diff = lhs[i] - rhs[j];
                    if (diff > 0)
                    {
                        return 1;
                    }
                    else if (diff < 0)
                    {
                        return -1;
                    }
                }

                return 0;
            }
        }
    }
}
