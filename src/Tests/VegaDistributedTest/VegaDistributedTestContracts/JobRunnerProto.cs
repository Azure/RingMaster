// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: JobRunnerProto.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Microsoft.Vega.JobRunnerProto {

  /// <summary>Holder for reflection information generated from JobRunnerProto.proto</summary>
  public static partial class JobRunnerProtoReflection {

    #region Descriptor
    /// <summary>File descriptor for JobRunnerProto.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static JobRunnerProtoReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChRKb2JSdW5uZXJQcm90by5wcm90bxIdTWljcm9zb2Z0LlZlZ2EuSm9iUnVu",
            "bmVyUHJvdG8aGURpc3RUZXN0Q29tbW9uUHJvdG8ucHJvdG8iUgoQR2V0Sm9i",
            "U3RhdGVSZXBseRI+Cghqb2JTdGF0ZRgBIAEoCzIsLk1pY3Jvc29mdC5WZWdh",
            "LkRpc3RUZXN0Q29tbW9uUHJvdG8uSm9iU3RhdGUiUAoUR2V0Sm9iTWV0cmlj",
            "c1JlcXVlc3QSEgoKbWV0cmljTmFtZRgBIAEoCRISCgpzdGFydEluZGV4GAIg",
            "ASgFEhAKCHBhZ2VTaXplGAMgASgFIigKEkdldEpvYk1ldHJpY3NSZXBseRIS",
            "Cgpqb2JNZXRyaWNzGAEgAygBIkIKH0dldFNlcnZpY2VJbnN0YW5jZUlkZW50",
            "aXR5UmVwbHkSHwoXc2VydmljZUluc3RhbmNlSWRlbnRpdHkYASABKAkiaQoP",
            "U3RhcnRKb2JSZXF1ZXN0EhAKCHNjZW5hcmlvGAEgASgJEkQKCnBhcmFtZXRl",
            "cnMYAiADKAsyMC5NaWNyb3NvZnQuVmVnYS5EaXN0VGVzdENvbW1vblByb3Rv",
            "LkpvYlBhcmFtZXRlcjLXBAoMSm9iUnVubmVyU3ZjEmoKEENhbmNlbFJ1bm5p",
            "bmdKb2ISKS5NaWNyb3NvZnQuVmVnYS5EaXN0VGVzdENvbW1vblByb3RvLkVt",
            "cHR5GikuTWljcm9zb2Z0LlZlZ2EuRGlzdFRlc3RDb21tb25Qcm90by5FbXB0",
            "eSIAEmsKC0dldEpvYlN0YXRlEikuTWljcm9zb2Z0LlZlZ2EuRGlzdFRlc3RD",
            "b21tb25Qcm90by5FbXB0eRovLk1pY3Jvc29mdC5WZWdhLkpvYlJ1bm5lclBy",
            "b3RvLkdldEpvYlN0YXRlUmVwbHkiABJ5Cg1HZXRKb2JNZXRyaWNzEjMuTWlj",
            "cm9zb2Z0LlZlZ2EuSm9iUnVubmVyUHJvdG8uR2V0Sm9iTWV0cmljc1JlcXVl",
            "c3QaMS5NaWNyb3NvZnQuVmVnYS5Kb2JSdW5uZXJQcm90by5HZXRKb2JNZXRy",
            "aWNzUmVwbHkiABKJAQoaR2V0U2VydmljZUluc3RhbmNlSWRlbnRpdHkSKS5N",
            "aWNyb3NvZnQuVmVnYS5EaXN0VGVzdENvbW1vblByb3RvLkVtcHR5Gj4uTWlj",
            "cm9zb2Z0LlZlZ2EuSm9iUnVubmVyUHJvdG8uR2V0U2VydmljZUluc3RhbmNl",
            "SWRlbnRpdHlSZXBseSIAEmcKCFN0YXJ0Sm9iEi4uTWljcm9zb2Z0LlZlZ2Eu",
            "Sm9iUnVubmVyUHJvdG8uU3RhcnRKb2JSZXF1ZXN0GikuTWljcm9zb2Z0LlZl",
            "Z2EuRGlzdFRlc3RDb21tb25Qcm90by5FbXB0eSIAYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Microsoft.Vega.DistTestCommonProto.DistTestCommonProtoReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Microsoft.Vega.JobRunnerProto.GetJobStateReply), global::Microsoft.Vega.JobRunnerProto.GetJobStateReply.Parser, new[]{ "JobState" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Microsoft.Vega.JobRunnerProto.GetJobMetricsRequest), global::Microsoft.Vega.JobRunnerProto.GetJobMetricsRequest.Parser, new[]{ "MetricName", "StartIndex", "PageSize" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Microsoft.Vega.JobRunnerProto.GetJobMetricsReply), global::Microsoft.Vega.JobRunnerProto.GetJobMetricsReply.Parser, new[]{ "JobMetrics" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Microsoft.Vega.JobRunnerProto.GetServiceInstanceIdentityReply), global::Microsoft.Vega.JobRunnerProto.GetServiceInstanceIdentityReply.Parser, new[]{ "ServiceInstanceIdentity" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Microsoft.Vega.JobRunnerProto.StartJobRequest), global::Microsoft.Vega.JobRunnerProto.StartJobRequest.Parser, new[]{ "Scenario", "Parameters" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class GetJobStateReply : pb::IMessage<GetJobStateReply> {
    private static readonly pb::MessageParser<GetJobStateReply> _parser = new pb::MessageParser<GetJobStateReply>(() => new GetJobStateReply());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<GetJobStateReply> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Microsoft.Vega.JobRunnerProto.JobRunnerProtoReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobStateReply() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobStateReply(GetJobStateReply other) : this() {
      JobState = other.jobState_ != null ? other.JobState.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobStateReply Clone() {
      return new GetJobStateReply(this);
    }

    /// <summary>Field number for the "jobState" field.</summary>
    public const int JobStateFieldNumber = 1;
    private global::Microsoft.Vega.DistTestCommonProto.JobState jobState_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Microsoft.Vega.DistTestCommonProto.JobState JobState {
      get { return jobState_; }
      set {
        jobState_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as GetJobStateReply);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(GetJobStateReply other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(JobState, other.JobState)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (jobState_ != null) hash ^= JobState.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (jobState_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(JobState);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (jobState_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(JobState);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(GetJobStateReply other) {
      if (other == null) {
        return;
      }
      if (other.jobState_ != null) {
        if (jobState_ == null) {
          jobState_ = new global::Microsoft.Vega.DistTestCommonProto.JobState();
        }
        JobState.MergeFrom(other.JobState);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (jobState_ == null) {
              jobState_ = new global::Microsoft.Vega.DistTestCommonProto.JobState();
            }
            input.ReadMessage(jobState_);
            break;
          }
        }
      }
    }

  }

  public sealed partial class GetJobMetricsRequest : pb::IMessage<GetJobMetricsRequest> {
    private static readonly pb::MessageParser<GetJobMetricsRequest> _parser = new pb::MessageParser<GetJobMetricsRequest>(() => new GetJobMetricsRequest());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<GetJobMetricsRequest> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Microsoft.Vega.JobRunnerProto.JobRunnerProtoReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobMetricsRequest() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobMetricsRequest(GetJobMetricsRequest other) : this() {
      metricName_ = other.metricName_;
      startIndex_ = other.startIndex_;
      pageSize_ = other.pageSize_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobMetricsRequest Clone() {
      return new GetJobMetricsRequest(this);
    }

    /// <summary>Field number for the "metricName" field.</summary>
    public const int MetricNameFieldNumber = 1;
    private string metricName_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string MetricName {
      get { return metricName_; }
      set {
        metricName_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "startIndex" field.</summary>
    public const int StartIndexFieldNumber = 2;
    private int startIndex_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int StartIndex {
      get { return startIndex_; }
      set {
        startIndex_ = value;
      }
    }

    /// <summary>Field number for the "pageSize" field.</summary>
    public const int PageSizeFieldNumber = 3;
    private int pageSize_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int PageSize {
      get { return pageSize_; }
      set {
        pageSize_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as GetJobMetricsRequest);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(GetJobMetricsRequest other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (MetricName != other.MetricName) return false;
      if (StartIndex != other.StartIndex) return false;
      if (PageSize != other.PageSize) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (MetricName.Length != 0) hash ^= MetricName.GetHashCode();
      if (StartIndex != 0) hash ^= StartIndex.GetHashCode();
      if (PageSize != 0) hash ^= PageSize.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (MetricName.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(MetricName);
      }
      if (StartIndex != 0) {
        output.WriteRawTag(16);
        output.WriteInt32(StartIndex);
      }
      if (PageSize != 0) {
        output.WriteRawTag(24);
        output.WriteInt32(PageSize);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (MetricName.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(MetricName);
      }
      if (StartIndex != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(StartIndex);
      }
      if (PageSize != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(PageSize);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(GetJobMetricsRequest other) {
      if (other == null) {
        return;
      }
      if (other.MetricName.Length != 0) {
        MetricName = other.MetricName;
      }
      if (other.StartIndex != 0) {
        StartIndex = other.StartIndex;
      }
      if (other.PageSize != 0) {
        PageSize = other.PageSize;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            MetricName = input.ReadString();
            break;
          }
          case 16: {
            StartIndex = input.ReadInt32();
            break;
          }
          case 24: {
            PageSize = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  public sealed partial class GetJobMetricsReply : pb::IMessage<GetJobMetricsReply> {
    private static readonly pb::MessageParser<GetJobMetricsReply> _parser = new pb::MessageParser<GetJobMetricsReply>(() => new GetJobMetricsReply());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<GetJobMetricsReply> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Microsoft.Vega.JobRunnerProto.JobRunnerProtoReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobMetricsReply() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobMetricsReply(GetJobMetricsReply other) : this() {
      jobMetrics_ = other.jobMetrics_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetJobMetricsReply Clone() {
      return new GetJobMetricsReply(this);
    }

    /// <summary>Field number for the "jobMetrics" field.</summary>
    public const int JobMetricsFieldNumber = 1;
    private static readonly pb::FieldCodec<double> _repeated_jobMetrics_codec
        = pb::FieldCodec.ForDouble(10);
    private readonly pbc::RepeatedField<double> jobMetrics_ = new pbc::RepeatedField<double>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<double> JobMetrics {
      get { return jobMetrics_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as GetJobMetricsReply);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(GetJobMetricsReply other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!jobMetrics_.Equals(other.jobMetrics_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= jobMetrics_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      jobMetrics_.WriteTo(output, _repeated_jobMetrics_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += jobMetrics_.CalculateSize(_repeated_jobMetrics_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(GetJobMetricsReply other) {
      if (other == null) {
        return;
      }
      jobMetrics_.Add(other.jobMetrics_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10:
          case 9: {
            jobMetrics_.AddEntriesFrom(input, _repeated_jobMetrics_codec);
            break;
          }
        }
      }
    }

  }

  public sealed partial class GetServiceInstanceIdentityReply : pb::IMessage<GetServiceInstanceIdentityReply> {
    private static readonly pb::MessageParser<GetServiceInstanceIdentityReply> _parser = new pb::MessageParser<GetServiceInstanceIdentityReply>(() => new GetServiceInstanceIdentityReply());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<GetServiceInstanceIdentityReply> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Microsoft.Vega.JobRunnerProto.JobRunnerProtoReflection.Descriptor.MessageTypes[3]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetServiceInstanceIdentityReply() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetServiceInstanceIdentityReply(GetServiceInstanceIdentityReply other) : this() {
      serviceInstanceIdentity_ = other.serviceInstanceIdentity_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public GetServiceInstanceIdentityReply Clone() {
      return new GetServiceInstanceIdentityReply(this);
    }

    /// <summary>Field number for the "serviceInstanceIdentity" field.</summary>
    public const int ServiceInstanceIdentityFieldNumber = 1;
    private string serviceInstanceIdentity_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string ServiceInstanceIdentity {
      get { return serviceInstanceIdentity_; }
      set {
        serviceInstanceIdentity_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as GetServiceInstanceIdentityReply);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(GetServiceInstanceIdentityReply other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ServiceInstanceIdentity != other.ServiceInstanceIdentity) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (ServiceInstanceIdentity.Length != 0) hash ^= ServiceInstanceIdentity.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (ServiceInstanceIdentity.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(ServiceInstanceIdentity);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (ServiceInstanceIdentity.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(ServiceInstanceIdentity);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(GetServiceInstanceIdentityReply other) {
      if (other == null) {
        return;
      }
      if (other.ServiceInstanceIdentity.Length != 0) {
        ServiceInstanceIdentity = other.ServiceInstanceIdentity;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            ServiceInstanceIdentity = input.ReadString();
            break;
          }
        }
      }
    }

  }

  public sealed partial class StartJobRequest : pb::IMessage<StartJobRequest> {
    private static readonly pb::MessageParser<StartJobRequest> _parser = new pb::MessageParser<StartJobRequest>(() => new StartJobRequest());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<StartJobRequest> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Microsoft.Vega.JobRunnerProto.JobRunnerProtoReflection.Descriptor.MessageTypes[4]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StartJobRequest() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StartJobRequest(StartJobRequest other) : this() {
      scenario_ = other.scenario_;
      parameters_ = other.parameters_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StartJobRequest Clone() {
      return new StartJobRequest(this);
    }

    /// <summary>Field number for the "scenario" field.</summary>
    public const int ScenarioFieldNumber = 1;
    private string scenario_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Scenario {
      get { return scenario_; }
      set {
        scenario_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "parameters" field.</summary>
    public const int ParametersFieldNumber = 2;
    private static readonly pb::FieldCodec<global::Microsoft.Vega.DistTestCommonProto.JobParameter> _repeated_parameters_codec
        = pb::FieldCodec.ForMessage(18, global::Microsoft.Vega.DistTestCommonProto.JobParameter.Parser);
    private readonly pbc::RepeatedField<global::Microsoft.Vega.DistTestCommonProto.JobParameter> parameters_ = new pbc::RepeatedField<global::Microsoft.Vega.DistTestCommonProto.JobParameter>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::Microsoft.Vega.DistTestCommonProto.JobParameter> Parameters {
      get { return parameters_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as StartJobRequest);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(StartJobRequest other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Scenario != other.Scenario) return false;
      if(!parameters_.Equals(other.parameters_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Scenario.Length != 0) hash ^= Scenario.GetHashCode();
      hash ^= parameters_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Scenario.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Scenario);
      }
      parameters_.WriteTo(output, _repeated_parameters_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Scenario.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Scenario);
      }
      size += parameters_.CalculateSize(_repeated_parameters_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(StartJobRequest other) {
      if (other == null) {
        return;
      }
      if (other.Scenario.Length != 0) {
        Scenario = other.Scenario;
      }
      parameters_.Add(other.parameters_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Scenario = input.ReadString();
            break;
          }
          case 18: {
            parameters_.AddEntriesFrom(input, _repeated_parameters_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code