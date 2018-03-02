﻿using System.Diagnostics.CodeAnalysis;

// We don't ngen anything...
[module: SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#DownloadUrlIntoLocation(System.String)")]
[module: SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#.ctor(Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence.IPersistedDataFactory`1<Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Node>,Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.SslWrapping,Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.SslWrapping)")]
[module: SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#_knownSessions")]
[module: SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#ProcessSessionInitialization(Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.ClientSession,Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RequestInit,System.UInt64)")]
[module: SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#ProcessSessionInitialization(Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RequestCall,Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.ClientSession)")]

[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence.EphemeralFactory+PersistedData.#RemoveChild(Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence.IPersistedData)", MessageId="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes.RmAssert.Fail(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#AppendAddChild(System.UInt64,System.UInt64,System.Int64,System.Int64)", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#ArchiveFile(System.String)", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#AppendTMTransaction(System.Int64,System.Byte[])", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#ArchiveAsync(System.String)", MessageId="System.Console.WriteLine(System.String,System.Object,System.Object)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#ArchiveAsync(System.String)", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#AppendRemove(System.UInt64,System.UInt64,System.Int64,System.Int64)", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#OnTimer(System.Object)", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#AppendSetData(System.UInt64,System.Byte[],System.Int64,System.Int64)", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#AppendCreate(Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence.IPersistedData,System.Int64,System.Int64)", MessageId="System.Console.WriteLine(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#AppendSetAcl(System.UInt64,System.Collections.Generic.IList`1<Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data.Acl>,System.Int64,System.Int64)", MessageId="System.Console.WriteLine(System.String)")]

[module: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#CreateUploaderMark(System.String)", MessageId="markPath")]

[module: SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#Dispose()")]

[module: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.WireBackup.#CreateFile(System.String&)")]
[module: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.BulkOperation.#DeserializeAllData(System.Byte[])")]

[module: SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.ClientSession.#_processMessage")]

[module: SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#Dispose()", MessageId="timerTermination")]
[module: SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#Dispose()", MessageId="_hasRoot")]

[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Node.#Watchers")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.CompleteNode.#Watchers")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence.IPersistedData.#Acl")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence.EphemeralFactory+PersistedData.#Acl")]
