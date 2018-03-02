// <copyright file="IMetric4D.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with four dimensions.
    /// </summary>
    public interface IMetric4D
    {
        void LogValue(
            long value,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4);
    }
}