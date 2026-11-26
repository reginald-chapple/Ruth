namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Whether a metric data point is an observed Measure or a target Goal.</summary>
public enum MetricValueType
{
    /// <summary>Observed operational measurement.</summary>
    Measure = 0,

    /// <summary>Target benchmark or goal.</summary>
    Goal = 1,
}
