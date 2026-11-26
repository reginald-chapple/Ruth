namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Persisted Metric definition.</summary>
/// <param name="Id">Metric identifier.</param>
/// <param name="Title">Metric title.</param>
/// <param name="Subtitle">Optional subtitle.</param>
/// <param name="MetricKey">Unique normalized metric key.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional MetricCategory identifier.</param>
/// <param name="IsCumulative">Whether values accumulate over time.</param>
/// <param name="SourceType">Manual, DataView, or SqlQuery.</param>
/// <param name="DataViewId">Optional DataView backing the calculation.</param>
/// <param name="SourceQueryKey">Optional allowlisted built-in SQL metric key.</param>
/// <param name="ScheduleCronExpression">Optional cron schedule expression.</param>
/// <param name="YAxisLabel">Optional Y-axis label.</param>
/// <param name="UnitLabel">Optional unit label (e.g., USD, People, %).</param>
/// <param name="IsActive">Active status.</param>
/// <param name="LastRunDateTimeUtc">Last scheduled or on-demand calculation timestamp.</param>
/// <param name="Partitions">Configured partition dimensions.</param>
public sealed record MetricResponse(
    Guid Id,
    string Title,
    string? Subtitle,
    string MetricKey,
    string? Description,
    Guid? CategoryId,
    bool IsCumulative,
    MetricSourceType SourceType,
    Guid? DataViewId,
    string? SourceQueryKey,
    string? ScheduleCronExpression,
    string? YAxisLabel,
    string? UnitLabel,
    bool IsActive,
    DateTimeOffset? LastRunDateTimeUtc,
    IReadOnlyList<MetricPartitionResponse> Partitions
);
