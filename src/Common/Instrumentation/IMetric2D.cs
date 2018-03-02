// <copyright file="IMetric2D.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with two dimensions.
    /// </summary>
    public interface IMetric2D
    {
        void LogValue(
            long value,
            string dimensionValue1,
            string dimensionValue2);
    }
}