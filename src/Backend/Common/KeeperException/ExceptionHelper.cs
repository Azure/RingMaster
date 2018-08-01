// <copyright file="ExceptionHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.KeeperException
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using GetDataOptions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions;
    using IGetDataOptionArgument = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.IGetDataOptionArgument;

    /// <summary>
    /// Class ExceptionHelper.
    /// </summary>
    public class ExceptionHelper
    {
        /// <summary>
        /// Gets the code string.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <returns>System.String.</returns>
        public static string GetCodeString(int rc)
        {
            switch (rc)
            {
                case (int)Code.Apierror:
                    return "Apierror";
                case (int)Code.Authfailed:
                    return "Authfailed";
                case (int)Code.Badarguments:
                    return "Badarguments";
                case (int)Code.Badversion:
                    return "Badversion";
                case (int)Code.Connectionloss:
                    return "Connectionloss";
                case (int)Code.Datainconsistency:
                    return "Datainconsistency";
                case (int)Code.Invalidacl:
                    return "Invalidacl";
                case (int)Code.Invalidcallback:
                    return "Invalidcallback";
                case (int)Code.Marshallingerror:
                    return "Marshallingerror";
                case (int)Code.Noauth:
                    return "Noauth";
                case (int)Code.Nochildrenforephemerals:
                    return "Nochildrenforephemerals";
                case (int)Code.Nodeexists:
                    return "Nodeexists";
                case (int)Code.Nonode:
                    return "Nonode";
                case (int)Code.Notempty:
                    return "Notempty";
                case (int)Code.Ok:
                    return "Ok";
                case (int)Code.Operationtimeout:
                    return "Operationtimeout";
                case (int)Code.Runtimeinconsistency:
                    return "Runtimeinconsistency";
                case (int)Code.Sessionexpired:
                    return "Sessionexpired";
                case (int)Code.Sessionmoved:
                    return "Sessionmoved";
                case (int)Code.Systemerror:
                    return "Systemerror";
                case (int)Code.TransactionNotAgreed:
                    return "TransactionNotAgreed";
                case (int)Code.Unimplemented:
                    return "Unimplemented";
                case (int)Code.Unknown:
                    return "Unknown";
            }

            return Enum.IsDefined(typeof(Code), rc) ? ((Code)rc).ToString() : rc.ToString();
        }

        /// <summary>
        /// Gets the exception associated to the result code.
        /// </summary>
        /// <param name="code">The rc.</param>
        /// <returns>Exception, or null if none.</returns>
        public static Exception GetException(Code code)
        {
            string message = $"error came from RM server: {code}";
            switch (code)
            {
                case Code.Ok:
                    return null;
                case Code.Nodeexists:
                    return null;
                case Code.Unimplemented:
                    return new NotSupportedException(message);
                case Code.Badarguments:
                    return new ArgumentException(message);
                case Code.Authfailed:
                case Code.Noauth:
                    return new AuthenticationException(message);
                case Code.Connectionloss:
                    return new IOException(message);
                case Code.Operationtimeout:
                case Code.Sessionexpired:
                    return new TimeoutException(message);
                case Code.Marshallingerror:
                    return new SerializationException(message);
                case Code.Badversion:
                case Code.Invalidacl:
                case Code.Nochildrenforephemerals:
                    return new InvalidDataException(message);
                case Code.Notempty:
                    return new ArgumentException(message);
                case Code.Nonode:
                    return new KeyNotFoundException(message);
                case Code.Invalidcallback:
                    return new InvalidOperationException(message);
                case Code.Apierror:
                case Code.Systemerror:
                case Code.Unknown:
                case Code.Runtimeinconsistency:
                case Code.Datainconsistency:
                case Code.Sessionmoved:
                    return new SessionMovedException(message);
                default:
                    return new UnknownException(message);
            }
        }

        /// <summary>
        /// Gets the exception associated to the result code.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <returns>Exception, or null if none.</returns>
        public static Exception GetException(int rc)
        {
            return GetException(GetCode(rc));
        }

        /// <summary>
        /// Gets the code.
        /// </summary>
        /// <param name="rc">The rc.</param>
        /// <returns>Code.</returns>
        public static Code GetCode(int rc)
        {
            Code code = Code.Unknown;
            if (Enum.IsDefined(typeof(Code), rc))
            {
                code = (Code)rc;
            }

            return code;
        }

        /// <summary>
        /// Converts enum to string for the request type
        /// </summary>
        /// <param name="requestType">Type of request</param>
        /// <returns>string representation of the request</returns>
        internal static string GetTypeString(RingMasterRequestType requestType)
        {
            switch (requestType)
            {
                case RingMasterRequestType.Check:
                    return "Check";
                case RingMasterRequestType.Create:
                    return "Create";
                case RingMasterRequestType.Delete:
                    return "Delete";
                case RingMasterRequestType.Exists:
                    return "Exists";
                case RingMasterRequestType.GetAcl:
                    return "GetAcl";
                case RingMasterRequestType.GetChildren:
                    return "GetChildren";
                case RingMasterRequestType.GetData:
                    return "GetData";
                case RingMasterRequestType.Init:
                    return "Init";
                case RingMasterRequestType.Multi:
                    return "Multi";
                case RingMasterRequestType.Batch:
                    return "Batch";
                case RingMasterRequestType.Nested:
                    return "Nested";
                case RingMasterRequestType.None:
                    return "None";
                case RingMasterRequestType.SetAcl:
                    return "SetAcl";
                case RingMasterRequestType.SetAuth:
                    return "SetAuth";
                case RingMasterRequestType.SetData:
                    return "SetData";
                case RingMasterRequestType.Sync:
                    return "Sync";
            }

            return requestType.ToString();
        }

        /// <summary>
        /// Session is moved
        /// </summary>
        [Serializable]
        public class SessionMovedException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SessionMovedException"/> class.
            /// </summary>
            /// <param name="message">Exception message</param>
            public SessionMovedException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Unknown exception
        /// </summary>
        [Serializable]
        public class UnknownException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="UnknownException"/> class.
            /// </summary>
            /// <param name="message">Exception message</param>
            public UnknownException(string message)
                : base(message)
            {
            }
        }
    }
}
