// <copyright file="RingMasterException.cs" company="Microsoft">
//     Copyright �  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;
    using System.Runtime.Serialization;
    using System.Text;
    using Requests;

    /// <summary>
    /// Exception type that can be thrown by RingMaster.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Exception factory methods are not documented")]
    public class RingMasterException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterException"/> class.
        /// </summary>
        /// <param name="errorCode">Code that identifies the error</param>
        /// <param name="message">Description of the error</param>
        private RingMasterException(Code errorCode, string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// Codes for possible failures.
        /// </summary>
        public enum Code
        {
            /// <summary>
            /// An API was not used correctly.
            /// </summary>
            Apierror,

            /// <summary>
            /// Client authentication failed.
            /// </summary>
            Authfailed,

            /// <summary>
            /// Invalid arguments.
            /// </summary>
            Badarguments,

            /// <summary>
            /// Version conflict.
            /// </summary>
            Badversion,

            /// <summary>
            /// Connection to the server has been lost.
            /// </summary>
            Connectionloss,

            /// <summary>
            /// A data inconsistency was found.
            /// </summary>
            Datainconsistency,

            /// <summary>
            /// Invalid <see cref="Acl"/> was specified.
            /// </summary>
            Invalidacl,

            /// <summary>
            /// Invalid callback specified 
            /// </summary>
            Invalidcallback,

            /// <summary>
            /// Error while marshaling or un-marshaling data.
            /// </summary>
            Marshallingerror,

            /// <summary>
            /// Not authenticated.
            /// </summary>
            Noauth,

            /// <summary>
            /// Ephemeral nodes are not allowed to have children.
            /// </summary>
            Nochildrenforephemerals,

            /// <summary>
            /// The node already exists.
            /// </summary>
            Nodeexists,

            /// <summary>
            /// Node does not exist.
            /// </summary>
            Nonode,

            /// <summary>
            /// The node has children.
            /// </summary>
            Notempty,

            /// <summary>
            /// Everything is OK.
            /// </summary>
            Ok,

            /// <summary>
            /// The request wasn't sent to backend and timed out from client side.
            /// </summary>
            Operationtimeout,

            /// <summary>
            /// A runtime inconsistency was found.
            /// </summary>
            Runtimeinconsistency,

            /// <summary>
            /// The session has been expired by the server.
            /// </summary>
            Sessionexpired,

            /// <summary>
            /// Session moved to another server, so operation is ignored.
            /// </summary>
            Sessionmoved,

            /// <summary>
            /// System and server-side errors.
            /// </summary>
            Systemerror,

            /// <summary>
            /// Operation is unimplemented.
            /// </summary>
            Unimplemented,

            /// <summary>
            /// Unknown error.
            /// </summary>
            Unknown,

            /// <summary>
            /// Participants did not agree on the transaction.
            /// </summary>
            TransactionNotAgreed,

            /// <summary>
            /// Operation timeout on server (the request comes with a max timeout for the execution queue at the server that was not met).
            /// </summary>
            Waitqueuetimeoutonserver,

            /// <summary>
            /// The server is in lockdown
            /// </summary>
            InLockDown,

            /// <summary>
            /// The requested node has too many children to be enumerated in a single request.
            /// </summary>
            TooManyChildren,

            /// <summary>
            /// The operation was cancelled.
            /// </summary>
            OperationCancelled,

            /// <summary>
            /// Operation timeout in backend
            /// </summary>
            ServerOperationTimeout
        }

        /// <summary>
        /// Gets the <see cref="Code"/> that identifies the error.
        /// </summary>
        public Code ErrorCode { get; private set; }

        /// <summary>
        /// Gets the exception that corresponds to the given RequestResponse.
        /// </summary>
        /// <param name="response">Request response</param>
        /// <returns>A <see cref="Exception"/> that corresponds to the response</returns>
        public static Exception GetException(RequestResponse response)
        {
            if (response != null)
            {
                Code errorCode = GetCode(response.ResultCode);
                if (errorCode != Code.Ok)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("ErrorCode: {0}", errorCode);
                    if (response.Content != null)
                    {
                        sb.AppendFormat(", Content: {0}", response.Content);
                    }

                    if (response.Stat != null)
                    {
                        sb.AppendFormat(", Stat: {0}", response.Stat.ToString());
                    }

                    return new RingMasterException(errorCode, sb.ToString());
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="Code"/> that corresponds to the given result code.
        /// </summary>
        /// <param name="resultCode">Result code</param>
        /// <returns>A <see cref="Code"/> that corresponds to the given result code</returns>
        public static Code GetCode(int resultCode)
        {
            return (Code)resultCode;
        }

        public static Exception Unimplemented(string message)
        {
            return new RingMasterException(Code.Unimplemented, message);
        }
        
        public static Exception BadArguments(string argumentName)
        {
            return new RingMasterException(Code.Badarguments, argumentName);
        }

        public static Exception AuthFailed()
        {
            return new RingMasterException(Code.Authfailed, "Authentication failed");
        }

        public static Exception NoAuth()
        {
            return new RingMasterException(Code.Noauth, "No Authentication");
        }

        public static Exception ConnectionLoss()
        {
            return new RingMasterException(Code.Connectionloss, "Connection lost");
        }

        public static Exception OperationTimeout()
        {
            return new RingMasterException(Code.Operationtimeout, "Operation timed out");
        }

        public static Exception SessionExpired()
        {
            return new RingMasterException(Code.Sessionexpired, "Session expired");
        }

        public static Exception MarshallingError()
        {
            return new RingMasterException(Code.Marshallingerror, "Serialization error");
        }

        public static Exception BadVersion()
        {
            return new RingMasterException(Code.Badversion, "Bad version");
        }

        public static Exception InvalidAcl()
        {
            return new RingMasterException(Code.Invalidacl, "Invalid ACL");
        }

        public static Exception ReportNoChildrenAllowedForEphemeralNodes()
        {
            return new RingMasterException(Code.Nochildrenforephemerals, "Ephemeral nodes cannot have children");
        }

        public static Exception NotEmpty()
        {
            return new RingMasterException(Code.Notempty, "Not Empty");
        }

        public static Exception NoNode(string path)
        {
            return new RingMasterException(Code.Nonode, string.Format("Node {0} was not found", path));
        }

        public static Exception NodeExists(string path)
        {
            return new RingMasterException(Code.Nodeexists, string.Format("Node {0} already exists", path));
        }

        public static Exception InvalidCallback()
        {
            return new RingMasterException(Code.Invalidcallback, "Invalid callback");
        }

        public static Exception ApiError()
        {
            return new RingMasterException(Code.Apierror, "Api error");
        }

        public static Exception SystemError()
        {
            return new RingMasterException(Code.Systemerror, "System error");
        }

        public static Exception Unknown()
        {
            return new RingMasterException(Code.Unknown, "Unknown error");
        }

        public static Exception RuntimeInconsistency()
        {
            return new RingMasterException(Code.Runtimeinconsistency, "Runtime inconsistency");
        }

        public static Exception DataInconsistency()
        {
            return new RingMasterException(Code.Datainconsistency, "Data inconsistency");
        }

        public static Exception SystemMoved()
        {
            return new RingMasterException(Code.Sessionmoved, "Session Moved");
        }

        /// <summary>
        /// ISerializable implementation.
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("ErrorCode", this.ErrorCode);
        }
    }
}