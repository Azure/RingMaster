// <copyright file="MockIfx.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

#pragma warning disable

namespace Microsoft.Cloud.InstrumentationFramework
{
    using System;

    public struct ErrorContext
    {
        public uint ErrorCode;
        public string ErrorMessage;
    }

    public class DefaultConfiguration
    {
        public static bool SetDefaultDimensionNamesValues(
            ref ErrorContext errorContext,
            uint length,
            string[] defaultDimensionNames,
            string[] defaultDimensionValues)
        {
            return true;
        }
    }

    public class IfxInitializer
    {
        public static void IfxInitialize(
            string sessionName,
            InstrumentationSpecification instrumentationSpec,
            AuditSpecification auditSpec)
        {
        }
    }

    public class MeasureMetric0D
    {
        public static MeasureMetric0D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            bool addDefaultDimension)
        {
            return new MeasureMetric0D();
        }

        public void LogValue(
            long value)
        {
        }
    }

    public class MeasureMetric1D
    {
        public static MeasureMetric1D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            bool addDefaultDimension)
        {
            return new MeasureMetric1D();
        }

        public void LogValue(
            long value,
            string dimensionValue0)
        {
        }
    }

    public class MeasureMetric2D
    {
        public static MeasureMetric2D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            bool addDefaultDimension)
        {
            return new MeasureMetric2D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1)
        {
        }
    }

    public class MeasureMetric3D
    {
        public static MeasureMetric3D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            bool addDefaultDimension)
        {
            return new MeasureMetric3D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2)
        {
        }
    }

    public class MeasureMetric4D
    {
        public static MeasureMetric4D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            string dimensionName3,
            bool addDefaultDimension)
        {
            return new MeasureMetric4D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3)
        {
        }
    }

    public class MeasureMetric5D
    {
        public static MeasureMetric5D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            string dimensionName3,
            string dimensionName4,
            bool addDefaultDimension)
        {
            return new MeasureMetric5D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4)
        {
        }
    }

    public class MeasureMetric6D
    {
        public static MeasureMetric6D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            string dimensionName3,
            string dimensionName4,
            string dimensionName5,
            bool addDefaultDimension)
        {
            return new MeasureMetric6D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5)
        {
        }
    }

    public class MeasureMetric7D
    {
        public static MeasureMetric7D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            string dimensionName3,
            string dimensionName4,
            string dimensionName5,
            string dimensionName6,
            bool addDefaultDimension)
        {
            return new MeasureMetric7D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5,
            string dimensionValue6)
        {
        }
    }

    public class MeasureMetric8D
    {
        public static MeasureMetric8D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            string dimensionName3,
            string dimensionName4,
            string dimensionName5,
            string dimensionName6,
            string dimensionName7,
            bool addDefaultDimension)
        {
            return new MeasureMetric8D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5,
            string dimensionValue6,
            string dimensionValue7)
        {
        }
    }

    public class MeasureMetric9D
    {
        public static MeasureMetric9D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            string dimensionName3,
            string dimensionName4,
            string dimensionName5,
            string dimensionName6,
            string dimensionName7,
            string dimensionName8,
            bool addDefaultDimension)
        {
            return new MeasureMetric9D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5,
            string dimensionValue6,
            string dimensionValue7,
            string dimensionValue8)
        {
        }
    }

    public class MeasureMetric10D
    {
        public static MeasureMetric10D Create(
            string accountName,
            string mdmNamespace,
            string metricName,
            string dimensionName0,
            string dimensionName1,
            string dimensionName2,
            string dimensionName3,
            string dimensionName4,
            string dimensionName5,
            string dimensionName6,
            string dimensionName7,
            string dimensionName8,
            string dimensionName9,
            bool addDefaultDimension)
        {
            return new MeasureMetric10D();
        }

        public void LogValue(
            long value,
            string dimensionValue0,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5,
            string dimensionValue6,
            string dimensionValue7,
            string dimensionValue8,
            string dimensionValue9)
        {
        }
    }

    public enum IfxTracingLevel
    {
        Critical = 1,
        Error,
        Warning,
        Informational,
        Verbose,
    }

    public sealed class InstrumentationSpecification
    {
        public bool EmitIfxMetricsEvents { get; set; }
        public string MonitoringAccountName { get; set; }
        public bool WriteIfxTracerDiskLogs { get; set; }
        public string TraceDirectory { get; set; }
        public uint MaxSizeTraceFileInKb { get; set; }
        public uint MaxSizeCompletedTraceFilesInMb { get; set; }
    }

    public class AuditSpecification
    {
        public TimeSpan? heartbeatSpan = null;
    }

    public class IfxTracer
    {
        public static void LogMessage(IfxTracingLevel level, string source, string message)
        {
        }
    }
}
