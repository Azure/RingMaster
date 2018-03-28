// <copyright file="IMetricsFactory.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// The MetricsFactory interface.
    /// </summary>
    public interface IMetricsFactory
    {
        /// <summary>
        /// Create a zero dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <returns>A zero dimensional metric</returns>
        IMetric0D Create0D(string metricName);

        /// <summary>
        /// Create a one dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <returns>A one dimensional metric</returns>
        IMetric1D Create1D(string metricName, string dimension1);

        /// <summary>
        /// Create a two dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <returns>A two dimensional metric</returns>
        IMetric2D Create2D(string metricName, string dimension1, string dimension2);

        /// <summary>
        /// Create a three dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <returns>A three dimensional metric</returns>
        IMetric3D Create3D(string metricName, string dimension1, string dimension2, string dimension3);

        /// <summary>
        /// Create a four dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <returns>A four dimensional metric</returns>
        IMetric4D Create4D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4);

        /// <summary>
        /// Create a five dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <param name="dimension5">Name of the fifth dimension</param>
        /// <returns>A five dimensional metric</returns>
        IMetric5D Create5D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5);

        /// <summary>
        /// Create a six dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <param name="dimension5">Name of the fifth dimension</param>
        /// <param name="dimension6">Name of the sixth dimension</param>
        /// <returns>A six dimensional metric</returns>
        IMetric6D Create6D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6);

        /// <summary>
        /// Create a seven dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <param name="dimension5">Name of the fifth dimension</param>
        /// <param name="dimension6">Name of the sixth dimension</param>
        /// <param name="dimension7">Name of the seventh dimension</param>
        /// <returns>A seven dimensional metric</returns>
        IMetric7D Create7D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7);

        /// <summary>
        /// Create an eight dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <param name="dimension5">Name of the fifth dimension</param>
        /// <param name="dimension6">Name of the sixth dimension</param>
        /// <param name="dimension7">Name of the seventh dimension</param>
        /// <param name="dimension8">Name of the eighth dimension</param>
        /// <returns>An eight dimensional metric</returns>
        IMetric8D Create8D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7, string dimension8);

        /// <summary>
        /// Create a nine dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <param name="dimension5">Name of the fifth dimension</param>
        /// <param name="dimension6">Name of the sixth dimension</param>
        /// <param name="dimension7">Name of the seventh dimension</param>
        /// <param name="dimension8">Name of the eighth dimension</param>
        /// <param name="dimension9">Name of the ninth dimension</param>
        /// <returns>A nine dimensional metric</returns>
        IMetric9D Create9D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7, string dimension8, string dimension9);

        /// <summary>
        /// Create a ten dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <param name="dimension5">Name of the fifth dimension</param>
        /// <param name="dimension6">Name of the sixth dimension</param>
        /// <param name="dimension7">Name of the seventh dimension</param>
        /// <param name="dimension8">Name of the eighth dimension</param>
        /// <param name="dimension9">Name of the ninth dimension</param>
        /// <param name="dimension10">Name of the tenth dimension</param>
        /// <returns>A ten dimensional metric</returns>
        IMetric10D Create10D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7, string dimension8, string dimension9, string dimension10);
    }
}