// <copyright file="TestTracer.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RSLValidation
{
    using System;

    public class TestTracer : MarshalByRefObject
    {
        public enum EventType
        {
            Generic = 0,
            ShutDownCalled = 1,
            CanBecomePrimaryCalled = 2,
            NotifyStatusSecondaryCalled = 3,
            PrimaryCommandExecuted = 4,
            ExecuteReplicatedRequestCalled = 5,
            LoadStateCalled = 6,
            SaveStateCalled = 7,
            ReplicateCommandCalled = 8,
            AcceptMessageFromReplicaCalled = 9,
            StateSavedCalled = 10,
            StateCopiedCalled = 11,
            NotifyPrimaryRecoveredCalled = 12,
            NotifyConfigurationChangedCalled = 13,
            BootstrapFinished = 14,
            CreateStateMachine = 15,
            DisposeStateMachine = 16,
            UnloadAppDomain = 17,
        }

        public virtual void OnEvent(string originator, EventType type, string message)
        {
            Console.WriteLine("{0} {1} {2}", originator, type, message);
        }
    }
}