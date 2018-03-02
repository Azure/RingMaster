// <copyright file="IMetric5D.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with five dimensions.
    /// </summary>
    public interface IMetric5D
    {
        void LogValue(
            long value,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5);
    }
}