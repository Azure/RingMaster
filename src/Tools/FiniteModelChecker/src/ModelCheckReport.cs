// <copyright file="ModelCheckReport.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Tools.FiniteModelChecker
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A report on the execution of a finite model-check against a system.
    /// </summary>
    /// <typeparam name="TConstants">The system constants.</typeparam>
    /// <typeparam name="TVariables">The system variables.</typeparam>
    public class ModelCheckReport<TConstants, TVariables>
        where TVariables : IVariables
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCheckReport{TConstants, TVariables}"/>
        /// class. This initializes a report where no invariants were violated.
        /// </summary>
        /// <param name="initialStateCount">Number of initial system states.</param>
        /// <param name="totalStateCount">Total number of system states discovered.</param>
        /// <param name="uniqueStateCount">Unique number of system states discovered.</param>
        public ModelCheckReport(long initialStateCount, long totalStateCount, long uniqueStateCount)
        {
            this.Success = true;
            this.InitialStateCount = initialStateCount;
            this.TotalStateCount = totalStateCount;
            this.UniqueStateCount = uniqueStateCount;
            this.SafetyInvariantsViolated = new List<InvariantReport<TConstants, TVariables>>();
            this.UnsafeState = default(TVariables);
            this.ExecutionTrace = new List<TVariables>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCheckReport{TConstants, TVariables}"/>
        /// class.
        /// </summary>
        /// <param name="success"><see cref="Success"/></param>
        /// <param name="initialStateCount"><see cref="InitialStateCount"/></param>
        /// <param name="totalStateCountCount"><see cref="TotalStateCount"/></param>
        /// <param name="uniqueStateCountCount"><see cref="UniqueStateCount"/></param>
        /// <param name="safetyInvariantsViolated"><see cref="SafetyInvariantsViolated"/></param>
        /// <param name="unsafeState"><see cref="UnsafeState"/></param>
        /// <param name="executionTrace"><see cref="ExecutionTrace"/></param>
        public ModelCheckReport(
            bool success,
            long initialStateCount,
            long totalStateCountCount,
            long uniqueStateCountCount,
            List<InvariantReport<TConstants, TVariables>> safetyInvariantsViolated,
            TVariables unsafeState,
            List<TVariables> executionTrace)
        {
            this.Success = success;
            this.InitialStateCount = initialStateCount;
            this.TotalStateCount = totalStateCountCount;
            this.UniqueStateCount = uniqueStateCountCount;
            this.SafetyInvariantsViolated = safetyInvariantsViolated;
            this.UnsafeState = unsafeState;
            this.ExecutionTrace = executionTrace;
        }

        /// <summary>
        /// Gets whether the safety invariants held for all reachable system states.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the number of initial states found.
        /// </summary>
        public long InitialStateCount { get; }

        /// <summary>
        /// Gets the total number of system states discovered (equivalent to number of edges).
        /// </summary>
        public long TotalStateCount { get; }

        /// <summary>
        /// Gets the number of unique system states discovered.
        /// </summary>
        public long UniqueStateCount { get; }

        /// <summary>
        /// Gets the list of all safety invariants violated during system model checking.
        /// </summary>
        public List<InvariantReport<TConstants, TVariables>> SafetyInvariantsViolated { get; }

        /// <summary>
        /// Gets the state for which the safety invariant(s) failed to hold.
        /// </summary>
        public TVariables UnsafeState { get; }

        /// <summary>
        /// Gets the system execution trace to reach the <see cref="UnsafeState"/> from the initial
        /// state.
        /// </summary>
        public List<TVariables> ExecutionTrace { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.Success)
            {
                sb.AppendLine("Success!");
                sb.AppendLine($"Found [{this.InitialStateCount}] initial state(s).");
                sb.AppendLine($"Found [{this.TotalStateCount}] total states, [{this.UniqueStateCount}] unique.");
            }
            else
            {
                sb.AppendLine("Failure!");
                sb.AppendLine($"Found [{this.InitialStateCount}] initial state(s).");
                sb.AppendLine("Failing safety invariant(s):");
                foreach (var invariant in this.SafetyInvariantsViolated)
                {
                    sb.AppendLine($"{invariant.Invariant}: [{invariant.Description}]");
                }

                sb.AppendLine("Failing state:");
                sb.AppendLine(this.UnsafeState.ToString());

                sb.AppendLine("State trace:");
                foreach (TVariables state in this.ExecutionTrace)
                {
                    sb.AppendLine(state.ToString());
                }
            }

            return sb.ToString();
        }
    }
}