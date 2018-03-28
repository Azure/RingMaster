// <copyright file="OpResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RingMaster.Data;
    using RingMaster.Requests;

    /// <summary>
    /// OpResult contains the result of an <see cref="Op"/>.
    /// </summary>
    public abstract class OpResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpResult"/> class.
        /// </summary>
        /// <param name="resultType">The type of <see cref="Op"/> that is associated with this result</param>
        /// <param name="errorCode">Error code of the result</param>
        protected internal OpResult(OpCode resultType, RingMasterException.Code errorCode = RingMasterException.Code.Ok)
        {
            this.ResultType = resultType;
            this.ErrCode = errorCode;
        }

        /// <summary>
        /// Gets the type of <see cref="Op"/> that is associated with this result.
        /// </summary>
        public OpCode ResultType { get; private set; }

        /// <summary>
        /// Gets the error code of the operation.
        /// </summary>
        /// <remarks>
        /// Returns Code.Ok only if all was executed correctly.
        /// in the case of Multi, it indicates the first error found.
        /// </remarks>
        public RingMasterException.Code ErrCode { get; private set; }

        /// <summary>
        /// Convert the given <see cref="OpResult"/> to an equivalent <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="operationResult"><see cref="OpResult"/> to convert</param>
        /// <returns><see cref="RequestResponse"/> that is equivalent to the given <see cref="OpResult"/></returns>
        public static RequestResponse ToResponse(OpResult operationResult)
        {
            if (operationResult == null)
            {
                throw new ArgumentNullException("operationResult");
            }

            var response = new RequestResponse();
            response.ResultCode = (int)RingMasterException.Code.Ok;
            switch (operationResult.ResultType)
            {
                case OpCode.Check:
                {
                    break;
                }

                case OpCode.Create:
                {
                    var createResult = (OpResult.CreateResult)operationResult;
                    response.Content = createResult.Path;
                    response.Stat = createResult.Stat;
                    break;
                }

                case OpCode.GetData:
                {
                    var getDataResult = (OpResult.GetDataResult)operationResult;
                    response.ResponsePath = getDataResult.Path;
                    response.Stat = getDataResult.Stat;
                    response.Content = getDataResult.Bytes;
                    break;
                }

                case OpCode.Delete:
                {
                    break;
                }

                case OpCode.SetData:
                {
                    var setDataResult = (OpResult.SetDataResult)operationResult;
                    response.Stat = setDataResult.Stat;
                    break;
                }

                case OpCode.SetACL:
                {
                    var setAclResult = (OpResult.SetAclResult)operationResult;
                    response.Stat = setAclResult.Stat;
                    break;
                }

                case OpCode.Multi:
                {
                    var runResult = (OpResult.RunResult)operationResult;
                    response.Content = runResult.Results;
                    break;
                }

                case OpCode.Error:
                {
                    var errorResult = (OpResult.ErrorResult)operationResult;
                    response.ResultCode = errorResult.ResultCode;
                    break;
                }
            }

            return response;
        }

        /// <summary>
        /// Gets the OpResult appropriate for the given RequestResponse and request type
        /// </summary>
        /// <param name="requestType">Type of request associated with the response</param>
        /// <param name="response">Response to convert into <see cref="OpResult"/></param>
        /// <returns>A <see cref="OpResult"/> that represents the response</returns>
        public static OpResult GetOpResult(RingMasterRequestType requestType, RequestResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.ResultCode != (int)RingMasterException.Code.Ok)
            {
                return new OpResult.ErrorResult(response.ResultCode);
            }

            switch (requestType)
            {
                case RingMasterRequestType.Check:
                    return new OpResult.CheckResult();
                case RingMasterRequestType.Create:
                    return new OpResult.CreateResult(response.Stat, (string)response.Content);
                case RingMasterRequestType.GetData:
                    return new OpResult.GetDataResult(response.Stat, (byte[])response.Content, response.ResponsePath);
                case RingMasterRequestType.Delete:
                    return new OpResult.DeleteResult();
                case RingMasterRequestType.SetData:
                    return new OpResult.SetDataResult(response.Stat);
                case RingMasterRequestType.Move:
                    return new OpResult.MoveResult(response.Stat, (string)response.Content);
                case RingMasterRequestType.SetAcl:
                    return new OpResult.SetAclResult(response.Stat);
                case RingMasterRequestType.Multi:
                case RingMasterRequestType.Batch:
                    return new OpResult.RunResult((List<OpResult>)response.Content);
            }

            return new OpResult.ErrorResult((int)RingMasterException.Code.Unimplemented);
        }

        /// <summary>
        /// Result of a <see cref="OpCode.Check"/> operation.
        /// </summary>
        public sealed class CheckResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CheckResult"/> class.
            /// </summary>
            public CheckResult()
                : base(OpCode.Check)
            {
            }
        }

        /// <summary>
        /// Result of a <see cref="OpCode.GetData"/> operation.
        /// </summary>
        public sealed class GetDataResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="GetDataResult"/> class.
            /// </summary>
            /// <param name="stat">The <see cref="IStat"/> of the node at the time GetData operation was executed</param>
            /// <param name="bytes">Content of the node</param>
            /// <param name="path">Path to the node</param>
            public GetDataResult(IStat stat, byte[] bytes, string path)
                : base(OpCode.GetData)
            {
                this.Stat = stat;
                this.Bytes = bytes;
                this.Path = path;
            }

            /// <summary>
            /// Gets the path to the node.
            /// </summary>
            public string Path { get; private set; }

            /// <summary>
            /// Gets the <see cref="IStat"/> associated with the node at the time of the GetData operation.
            /// </summary>
            public IStat Stat { get; private set; }

            /// <summary>
            /// Gets the content of the node.
            /// </summary>
            public byte[] Bytes { get; private set; }
        }

        /// <summary>
        /// Result of a <see cref="OpCode.Create"/> operation.
        /// </summary>
        public sealed class CreateResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CreateResult"/> class.
            /// </summary>
            /// <param name="stat">The <see cref="IStat"/> of the newly created node</param>
            /// <param name="path">Path of the newly created node</param>
            public CreateResult(IStat stat, string path)
                : base(OpCode.Create)
            {
                this.Path = path;
                this.Stat = stat;
            }

            /// <summary>
            /// Gets the path of the newly created node.
            /// </summary>
            public string Path { get; private set; }

            /// <summary>
            /// Gets the <see cref="IStat"/> associated with the newly created node.
            /// </summary>
            public IStat Stat { get; private set; }
        }

        /// <summary>
        /// Result of a <see cref="OpCode.Delete"/> operation.
        /// </summary>
        public sealed class DeleteResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DeleteResult"/> class.
            /// </summary>
            public DeleteResult()
                : base(OpCode.Delete)
            {
            }
        }

        /// <summary>
        /// Result of a <see cref="OpCode.SetData"/> operation.
        /// </summary>
        public sealed class SetDataResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SetDataResult"/> class.
            /// </summary>
            /// <param name="stat">The <see cref="IStat"/> of the node after the SetData operation</param>
            public SetDataResult(IStat stat)
                : base(OpCode.SetData)
            {
                this.Stat = stat;
            }

            /// <summary>
            /// Gets the <see cref="IStat"/> of the node after the SetData operation was completed.
            /// </summary>
            public IStat Stat { get; private set; }
        }

        /// <summary>
        /// Result of a <see cref="OpCode.Create"/> operation.
        /// </summary>
        public sealed class MoveResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MoveResult"/> class.
            /// </summary>
            /// <param name="stat">The <see cref="IStat"/> of the newly created node</param>
            /// <param name="path">Path of the newly created node</param>
            public MoveResult(IStat stat, string path)
                : base(OpCode.Move)
            {
                this.DstPath = path;
                this.Stat = stat;
            }

            /// <summary>
            /// Gets the path of the newly created node.
            /// </summary>
            public string DstPath { get; private set; }

            /// <summary>
            /// Gets the <see cref="IStat"/> associated with the newly created node.
            /// </summary>
            public IStat Stat { get; private set; }
        }

        /// <summary>
        /// Result of a <see cref="OpCode.Multi"/> operation.
        /// </summary>
        public sealed class RunResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RunResult"/> class.
            /// </summary>
            /// <param name="results">Results of individual operations contained in the Multi operation</param>
            public RunResult(List<OpResult> results)
                : base(OpCode.Multi, GetFirstError(results))
            {
                this.Results = results;
            }

            /// <summary>
            /// Gets the results of individual operations.
            /// </summary>
            public List<OpResult> Results { get; private set; }

            /// <summary>
            /// Gets the error code of the first failed operation or Ok if all operations have succeeded.
            /// </summary>
            /// <param name="results">Results list from which the error code of the first failed operation must be retrieved</param>
            /// <returns>Error code of the first failed operation or Ok</returns>
            private static RingMasterException.Code GetFirstError(IEnumerable<OpResult> results)
            {
                RingMasterException.Code c = RingMasterException.Code.Ok;

                foreach (OpResult r in results)
                {
                    if (c != RingMasterException.Code.Ok)
                    {
                        c = r.ErrCode;
                        break;
                    }
                }

                return c;
            }
        }

        /// <summary>
        /// Result of a <see cref="OpCode.SetACL"/> operation.
        /// </summary>
        public sealed class SetAclResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SetAclResult"/> class.
            /// </summary>
            /// <param name="stat">The <see cref="IStat"/> of the node after the operation was completed</param>
            public SetAclResult(IStat stat)
                : base(OpCode.SetACL)
            {
                this.Stat = stat;
            }

            /// <summary>
            /// Gets the <see cref="IStat" /> of the node after the operation was completed.
            /// </summary>
            public IStat Stat { get; private set; }
        }

        /// <summary>
        /// Result that represents a failed operation.
        /// </summary>
        public sealed class ErrorResult : OpResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ErrorResult"/> class.
            /// </summary>
            /// <param name="resultCode">Result code of the failure</param>
            public ErrorResult(int resultCode)
                : base(OpCode.Error, RingMasterException.GetCode(resultCode))
            {
                this.ResultCode = resultCode;
            }

            /// <summary>
            /// Gets the result code associated with the operation.
            /// </summary>
            public int ResultCode { get; private set; }
        }
    }
}