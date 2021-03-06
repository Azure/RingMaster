// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: DistributedJobControllerProto.proto
// </auto-generated>
#pragma warning disable 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace Microsoft.Vega.DistributedJobControllerProto {
  public static partial class DistributedJobControllerSvc
  {
    static readonly string __ServiceName = "Microsoft.Vega.DistributedJobControllerProto.DistributedJobControllerSvc";

    static readonly grpc::Marshaller<global::Microsoft.Vega.DistTestCommonProto.Empty> __Marshaller_Empty = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Microsoft.Vega.DistTestCommonProto.Empty.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply> __Marshaller_GetJobStatesReply = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest> __Marshaller_GetJobMetricsRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply> __Marshaller_GetJobMetricsReply = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply> __Marshaller_GetServiceInstanceIdentitiesReply = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest> __Marshaller_StartJobRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest.Parser.ParseFrom);

    static readonly grpc::Method<global::Microsoft.Vega.DistTestCommonProto.Empty, global::Microsoft.Vega.DistTestCommonProto.Empty> __Method_CancelRunningJob = new grpc::Method<global::Microsoft.Vega.DistTestCommonProto.Empty, global::Microsoft.Vega.DistTestCommonProto.Empty>(
        grpc::MethodType.Unary,
        __ServiceName,
        "CancelRunningJob",
        __Marshaller_Empty,
        __Marshaller_Empty);

    static readonly grpc::Method<global::Microsoft.Vega.DistTestCommonProto.Empty, global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply> __Method_GetJobStates = new grpc::Method<global::Microsoft.Vega.DistTestCommonProto.Empty, global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetJobStates",
        __Marshaller_Empty,
        __Marshaller_GetJobStatesReply);

    static readonly grpc::Method<global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest, global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply> __Method_GetJobMetrics = new grpc::Method<global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest, global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetJobMetrics",
        __Marshaller_GetJobMetricsRequest,
        __Marshaller_GetJobMetricsReply);

    static readonly grpc::Method<global::Microsoft.Vega.DistTestCommonProto.Empty, global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply> __Method_GetServiceInstanceIdentities = new grpc::Method<global::Microsoft.Vega.DistTestCommonProto.Empty, global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetServiceInstanceIdentities",
        __Marshaller_Empty,
        __Marshaller_GetServiceInstanceIdentitiesReply);

    static readonly grpc::Method<global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest, global::Microsoft.Vega.DistTestCommonProto.Empty> __Method_StartJob = new grpc::Method<global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest, global::Microsoft.Vega.DistTestCommonProto.Empty>(
        grpc::MethodType.Unary,
        __ServiceName,
        "StartJob",
        __Marshaller_StartJobRequest,
        __Marshaller_Empty);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Microsoft.Vega.DistributedJobControllerProto.DistributedJobControllerProtoReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of DistributedJobControllerSvc</summary>
    public abstract partial class DistributedJobControllerSvcBase
    {
      public virtual global::System.Threading.Tasks.Task<global::Microsoft.Vega.DistTestCommonProto.Empty> CancelRunningJob(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply> GetJobStates(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply> GetJobMetrics(global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply> GetServiceInstanceIdentities(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Microsoft.Vega.DistTestCommonProto.Empty> StartJob(global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for DistributedJobControllerSvc</summary>
    public partial class DistributedJobControllerSvcClient : grpc::ClientBase<DistributedJobControllerSvcClient>
    {
      /// <summary>Creates a new client for DistributedJobControllerSvc</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public DistributedJobControllerSvcClient(grpc::Channel channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for DistributedJobControllerSvc that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public DistributedJobControllerSvcClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected DistributedJobControllerSvcClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected DistributedJobControllerSvcClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual global::Microsoft.Vega.DistTestCommonProto.Empty CancelRunningJob(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return CancelRunningJob(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Microsoft.Vega.DistTestCommonProto.Empty CancelRunningJob(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_CancelRunningJob, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistTestCommonProto.Empty> CancelRunningJobAsync(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return CancelRunningJobAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistTestCommonProto.Empty> CancelRunningJobAsync(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_CancelRunningJob, null, options, request);
      }
      public virtual global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply GetJobStates(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetJobStates(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply GetJobStates(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetJobStates, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply> GetJobStatesAsync(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetJobStatesAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistributedJobControllerProto.GetJobStatesReply> GetJobStatesAsync(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetJobStates, null, options, request);
      }
      public virtual global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply GetJobMetrics(global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetJobMetrics(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply GetJobMetrics(global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetJobMetrics, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply> GetJobMetricsAsync(global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetJobMetricsAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsReply> GetJobMetricsAsync(global::Microsoft.Vega.DistributedJobControllerProto.GetJobMetricsRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetJobMetrics, null, options, request);
      }
      public virtual global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply GetServiceInstanceIdentities(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetServiceInstanceIdentities(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply GetServiceInstanceIdentities(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetServiceInstanceIdentities, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply> GetServiceInstanceIdentitiesAsync(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetServiceInstanceIdentitiesAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistributedJobControllerProto.GetServiceInstanceIdentitiesReply> GetServiceInstanceIdentitiesAsync(global::Microsoft.Vega.DistTestCommonProto.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetServiceInstanceIdentities, null, options, request);
      }
      public virtual global::Microsoft.Vega.DistTestCommonProto.Empty StartJob(global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return StartJob(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Microsoft.Vega.DistTestCommonProto.Empty StartJob(global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_StartJob, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistTestCommonProto.Empty> StartJobAsync(global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return StartJobAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Microsoft.Vega.DistTestCommonProto.Empty> StartJobAsync(global::Microsoft.Vega.DistributedJobControllerProto.StartJobRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_StartJob, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override DistributedJobControllerSvcClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new DistributedJobControllerSvcClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(DistributedJobControllerSvcBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_CancelRunningJob, serviceImpl.CancelRunningJob)
          .AddMethod(__Method_GetJobStates, serviceImpl.GetJobStates)
          .AddMethod(__Method_GetJobMetrics, serviceImpl.GetJobMetrics)
          .AddMethod(__Method_GetServiceInstanceIdentities, serviceImpl.GetServiceInstanceIdentities)
          .AddMethod(__Method_StartJob, serviceImpl.StartJob).Build();
    }

  }
}
#endregion
