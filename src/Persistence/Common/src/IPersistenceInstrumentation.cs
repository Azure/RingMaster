// <copyright file="IPersistenceInstrumentation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence
{
    using System;

#pragma warning disable CS1591, SA1600
    /// <summary>
    /// Instrumentation for Persistence.
    /// </summary>
    public interface IPersistenceInstrumentation
    {
        void LoadTreeCompleted(TimeSpan elapsed);

        void ProcessLoadCompleted();

        void ProcessLoadFailed();

        void ProcessAddCompleted();

        void ProcessAddFailed();

        void ProcessUpdateCompleted();

        void ProcessUpdateFailed();

        void ProcessRemoveCompleted();

        void ProcessRemoveFailed();

        void AddRequested(TimeSpan elapsed);

        void UpdateRequested(TimeSpan elapsed);

        void RemoveRequested(TimeSpan elapsed);

        void ChangeListCommitted(TimeSpan elapsed);

        void ChangeListCommitFailed();

        void ChangeListAborted(TimeSpan elapsed);

        void DataLoadCompleted(long dataCount, long duplicatesCount, long orphansCount);
    }
#pragma warning restore
}
