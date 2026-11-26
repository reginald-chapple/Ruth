namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Source strategy used to populate MetricValue records.</summary>
public enum MetricSourceType
{
    /// <summary>Manually entered via OperationalMetricsEntry.</summary>
    Manual = 0,

    /// <summary>Calculated from a DataView result count.</summary>
    DataView = 1,

    /// <summary>Calculated from an allowlisted analytical SQL query key.</summary>
    SqlQuery = 2,
}
