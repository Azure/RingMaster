// <copyright file="FxCopRulesModifications.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

#pragma warning disable SA1404

using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#Dispose()")]
[module: SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RingMasterBackendCore.#Dispose()", MessageId="timerTermination")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Node.#Watchers")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.CompleteNode.#Watchers")]