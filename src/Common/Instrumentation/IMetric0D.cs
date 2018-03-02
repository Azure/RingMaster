// <copyright file="IMetric0D.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with zero dimensions.
    /// </summary>
    public interface IMetric0D
    {
        void LogValue(long value);
    }
}