// <copyright file="IMetric1D.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with one dimension.
    /// </summary>
    public interface IMetric1D
    {
        void LogValue(long value, string dimensionValue1);
    }
}