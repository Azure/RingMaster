// <copyright file="IfxMetricsFactory.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.IfxInstrumentation
{
    using System;
    using System.Diagnostics;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    /// An Implementation of the <see cref="IMetricsFactory"/> interface that uses
    /// InstrumentationFramework metrics.
    /// </summary>
    internal sealed class IfxMetricsFactory : IMetricsFactory
    {
        /// <summary>
        /// The list of default dimensions that will be associated with all metrics created by this factory.
        /// </summary>
        private static readonly string[] DefaultDimensionNames = new string[] { "environment", "tenant", "role", "roleInstance" };

        /// <summary>
        /// The name of the account that will be used for the metrics.
        /// </summary>
        private readonly string mdmAccountName;

        /// <summary>
        /// The namespace that will be used for the metrics
        /// </summary>
        private readonly string mdmNamespace;

        /// <summary>
        /// Initializes a new instance of the <see cref="IfxMetricsFactory"/> class.
        /// </summary>
        /// <param name="mdmAccountName">The name of the MDM account.</param>
        /// <param name="mdmNamespace">The name of the MDM namespace.</param>
        /// <param name="environment">The type of environment, e.g. <c>Prod</c>, <c>Test</c> or <c>Int</c></param>
        /// <param name="tenantName">Name of the tenant</param>
        /// <param name="roleName">Name of the role</param>
        /// <param name="roleInstanceId">Id of the RoleInstance</param>
        public IfxMetricsFactory(string mdmAccountName, string mdmNamespace, string environment, string tenantName, string roleName, string roleInstanceId)
            : this(mdmAccountName, mdmNamespace, DefaultDimensionNames, new string[] { environment, tenantName, roleName, roleInstanceId })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IfxMetricsFactory"/> class.
        /// </summary>
        /// <param name="mdmAccountName">The name of the MDM account.</param>
        /// <param name="mdmNamespace">The name of the MDM namespace.</param>
        /// <param name="defaultDimensionNames">Default dimensions name - can be tenantName, RoleName, RoleId, region, etc.</param>
        /// <param name="defaultDimenstionValues">Values for default dimensions.</param>
        public IfxMetricsFactory(string mdmAccountName, string mdmNamespace, string[] defaultDimensionNames, string[] defaultDimenstionValues)
        {
            if (string.IsNullOrEmpty(mdmAccountName))
            {
                throw new ArgumentNullException("mdmAccountName");
            }

            if (string.IsNullOrEmpty(mdmNamespace))
            {
                throw new ArgumentNullException("mdmNamespace");
            }

            if (defaultDimensionNames == null)
            {
                throw new ArgumentNullException("defaultDimensionNames");
            }

            if (defaultDimenstionValues == null)
            {
                throw new ArgumentNullException("defaultDimenstionValues");
            }

            this.mdmAccountName = mdmAccountName;
            this.mdmNamespace = mdmNamespace;

            ErrorContext errContext = new ErrorContext();

            DefaultConfiguration.SetDefaultDimensionNamesValues(
                    ref errContext,
                    (uint)defaultDimensionNames.Length,
                    defaultDimensionNames,
                    defaultDimenstionValues);

            if (errContext.ErrorCode != 0)
            {
                Trace.TraceError("Failed to set default dimension names values. ErrorCode: {0}, ErrorMessage: {1}", errContext.ErrorCode, errContext.ErrorMessage);
            }
            else
            {
                Trace.WriteLine("Successfully set default dimensions for MDM metrics");
            }
        }

        /// <summary>
        /// Create a zero dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <returns>A zero dimensional metric</returns>
        public IMetric0D Create0D(string metricName)
        {
            return new Metric0DWrapper(this.mdmAccountName, this.mdmNamespace, metricName);
        }

        /// <summary>
        /// Create a one dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <returns>A one dimensional metric</returns>
        public IMetric1D Create1D(string metricName, string dimension1)
        {
            return new Metric1DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1);
        }

        /// <summary>
        /// Create a two dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <returns>A two dimensional metric</returns>
        public IMetric2D Create2D(string metricName, string dimension1, string dimension2)
        {
            return new Metric2DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2);
        }

        /// <summary>
        /// Create a three dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <returns>A three dimensional metric</returns>
        public IMetric3D Create3D(string metricName, string dimension1, string dimension2, string dimension3)
        {
            return new Metric3DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3);
        }

        /// <summary>
        /// Create a four dimensional metric.
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="dimension1">Name of the first dimension</param>
        /// <param name="dimension2">Name of the second dimension</param>
        /// <param name="dimension3">Name of the third dimension</param>
        /// <param name="dimension4">Name of the fourth dimension</param>
        /// <returns>A four dimensional metric</returns>
        public IMetric4D Create4D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4)
        {
            return new Metric4DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3, dimension4);
        }

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
        public IMetric5D Create5D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5)
        {
            return new Metric5DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3, dimension4, dimension5);
        }

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
        public IMetric6D Create6D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6)
        {
            return new Metric6DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3, dimension4, dimension5, dimension6);
        }

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
        public IMetric7D Create7D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7)
        {
            return new Metric7DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3, dimension4, dimension5, dimension6, dimension7);
        }

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
        public IMetric8D Create8D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7, string dimension8)
        {
            return new Metric8DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3, dimension4, dimension5, dimension6, dimension7, dimension8);
        }

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
        public IMetric9D Create9D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7, string dimension8, string dimension9)
        {
            return new Metric9DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3, dimension4, dimension5, dimension6, dimension7, dimension8, dimension9);
        }

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
        public IMetric10D Create10D(string metricName, string dimension1, string dimension2, string dimension3, string dimension4, string dimension5, string dimension6, string dimension7, string dimension8, string dimension9, string dimension10)
        {
            return new Metric10DWrapper(this.mdmAccountName, this.mdmNamespace, metricName, dimension1, dimension2, dimension3, dimension4, dimension5, dimension6, dimension7, dimension8, dimension9, dimension10);
        }

        private sealed class Metric0DWrapper : IMetric0D
        {
            private readonly MeasureMetric0D metric;

            public Metric0DWrapper(string mdmAccountName, string mdmNamespace, string metricName)
            {
                this.metric = MeasureMetric0D.Create(mdmAccountName, mdmNamespace, metricName, addDefaultDimension: true);
            }

            public void LogValue(long value)
            {
                this.metric.LogValue(value);
            }
        }

        private sealed class Metric1DWrapper : IMetric1D
        {
            private readonly MeasureMetric1D metric;

            public Metric1DWrapper(string mdmAccountName, string mdmNamespace, string metricName, string dimension1Name)
            {
                this.metric = MeasureMetric1D.Create(mdmAccountName, mdmNamespace, metricName, dimension1Name, addDefaultDimension: true);
            }

            public void LogValue(long value, string dimension1Value)
            {
                this.metric.LogValue(value, dimension1Value);
            }
        }

        private sealed class Metric2DWrapper : IMetric2D
        {
            private readonly MeasureMetric2D metric;

            public Metric2DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name)
            {
                this.metric = MeasureMetric2D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value);
            }
        }

        private sealed class Metric3DWrapper : IMetric3D
        {
            private readonly MeasureMetric3D metric;

            public Metric3DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name)
            {
                this.metric = MeasureMetric3D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value);
            }
        }

        private sealed class Metric4DWrapper : IMetric4D
        {
            private readonly MeasureMetric4D metric;

            public Metric4DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name,
                string dimension4Name)
            {
                this.metric = MeasureMetric4D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    dimension4Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value,
                string dimension4Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value,
                    dimension4Value);
            }
        }

        private sealed class Metric5DWrapper : IMetric5D
        {
            private readonly MeasureMetric5D metric;

            public Metric5DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name,
                string dimension4Name,
                string dimension5Name)
            {
                this.metric = MeasureMetric5D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    dimension4Name,
                    dimension5Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value,
                string dimension4Value,
                string dimension5Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value,
                    dimension4Value,
                    dimension5Value);
            }
        }

        private sealed class Metric6DWrapper : IMetric6D
        {
            private readonly MeasureMetric6D metric;

            public Metric6DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name,
                string dimension4Name,
                string dimension5Name,
                string dimension6Name)
            {
                this.metric = MeasureMetric6D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    dimension4Name,
                    dimension5Name,
                    dimension6Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value,
                string dimension4Value,
                string dimension5Value,
                string dimension6Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value,
                    dimension4Value,
                    dimension5Value,
                    dimension6Value);
            }
        }

        private sealed class Metric7DWrapper : IMetric7D
        {
            private readonly MeasureMetric7D metric;

            public Metric7DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name,
                string dimension4Name,
                string dimension5Name,
                string dimension6Name,
                string dimension7Name)
            {
                this.metric = MeasureMetric7D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    dimension4Name,
                    dimension5Name,
                    dimension6Name,
                    dimension7Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value,
                string dimension4Value,
                string dimension5Value,
                string dimension6Value,
                string dimension7Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value,
                    dimension4Value,
                    dimension5Value,
                    dimension6Value,
                    dimension7Value);
            }
        }

        private sealed class Metric8DWrapper : IMetric8D
        {
            private readonly MeasureMetric8D metric;

            public Metric8DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name,
                string dimension4Name,
                string dimension5Name,
                string dimension6Name,
                string dimension7Name,
                string dimension8Name)
            {
                this.metric = MeasureMetric8D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    dimension4Name,
                    dimension5Name,
                    dimension6Name,
                    dimension7Name,
                    dimension8Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value,
                string dimension4Value,
                string dimension5Value,
                string dimension6Value,
                string dimension7Value,
                string dimension8Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value,
                    dimension4Value,
                    dimension5Value,
                    dimension6Value,
                    dimension7Value,
                    dimension8Value);
            }
        }

        private sealed class Metric9DWrapper : IMetric9D
        {
            private readonly MeasureMetric9D metric;

            public Metric9DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name,
                string dimension4Name,
                string dimension5Name,
                string dimension6Name,
                string dimension7Name,
                string dimension8Name,
                string dimension9Name)
            {
                this.metric = MeasureMetric9D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    dimension4Name,
                    dimension5Name,
                    dimension6Name,
                    dimension7Name,
                    dimension8Name,
                    dimension9Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value,
                string dimension4Value,
                string dimension5Value,
                string dimension6Value,
                string dimension7Value,
                string dimension8Value,
                string dimension9Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value,
                    dimension4Value,
                    dimension5Value,
                    dimension6Value,
                    dimension7Value,
                    dimension8Value,
                    dimension9Value);
            }
        }

        private sealed class Metric10DWrapper : IMetric10D
        {
            private readonly MeasureMetric10D metric;

            public Metric10DWrapper(
                string mdmAccountName,
                string mdmNamespace,
                string metricName,
                string dimension1Name,
                string dimension2Name,
                string dimension3Name,
                string dimension4Name,
                string dimension5Name,
                string dimension6Name,
                string dimension7Name,
                string dimension8Name,
                string dimension9Name,
                string dimension10Name)
            {
                this.metric = MeasureMetric10D.Create(
                    mdmAccountName,
                    mdmNamespace,
                    metricName,
                    dimension1Name,
                    dimension2Name,
                    dimension3Name,
                    dimension4Name,
                    dimension5Name,
                    dimension6Name,
                    dimension7Name,
                    dimension8Name,
                    dimension9Name,
                    dimension10Name,
                    addDefaultDimension: true);
            }

            public void LogValue(
                long value,
                string dimension1Value,
                string dimension2Value,
                string dimension3Value,
                string dimension4Value,
                string dimension5Value,
                string dimension6Value,
                string dimension7Value,
                string dimension8Value,
                string dimension9Value,
                string dimension10Value)
            {
                this.metric.LogValue(
                    value,
                    dimension1Value,
                    dimension2Value,
                    dimension3Value,
                    dimension4Value,
                    dimension5Value,
                    dimension6Value,
                    dimension7Value,
                    dimension8Value,
                    dimension9Value,
                    dimension10Value);
            }
        }
    }
}