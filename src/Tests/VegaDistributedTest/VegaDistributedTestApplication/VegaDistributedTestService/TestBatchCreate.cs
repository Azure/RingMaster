// <copyright file="TestBatchCreate.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Vega.Test.Helpers;
    using DistTestCommonProto;

    /// <summary>
    /// the batch create test
    /// </summary>
    /// <seealso cref="Microsoft.Vega.DistributedTest.TestBase" />
    internal sealed class TestBatchCreate : TestBase
    {
        /// <inheritdoc/>
        protected override Task RunTest(JobState jobState, CancellationToken cancellation)
        {
            jobState.Started = true;
            jobState.Status = "start batch creating nodes";

            var rate = this.TestBatchOrMultiCreate(true, OperationType.BatchCreate, jobState).GetAwaiter().GetResult();
            this.Log($"Node batch create rate: {rate:G4} /sec");

            jobState.Passed = rate > 0;

            return Task.FromResult(0);
        }
    }
}
