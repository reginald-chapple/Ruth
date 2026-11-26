using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Creates a Metric definition with optional partitions and scheduled calculation source.</summary>
/// <param name="Title">Metric title.</param>
/// <param name="MetricKey">Unique normalized key.</param>
/// <param name="SourceType">Manual, DataView, or SqlQuery.</param>
/// <param name="Subtitle">Optional subtitle.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional MetricCategory identifier.</param>
/// <param name="IsCumulative">Whether cumulative.</param>
/// <param name="DataViewId">Optional DataView identifier.</param>
/// <param name="SourceQueryKey">Optional allowlisted built-in query key.</param>
/// <param name="ScheduleCronExpression">Optional cron expression.</param>
/// <param name="YAxisLabel">Optional Y-axis label.</param>
/// <param name="UnitLabel">Optional unit label.</param>
/// <param name="Partitions">Optional partition dimensions.</param>
public sealed record CreateMetricCommand(
    string Title,
    string MetricKey,
    MetricSourceType SourceType = MetricSourceType.Manual,
    string? Subtitle = null,
    string? Description = null,
    Guid? CategoryId = null,
    bool IsCumulative = false,
    Guid? DataViewId = null,
    string? SourceQueryKey = null,
    string? ScheduleCronExpression = null,
    string? YAxisLabel = null,
    string? UnitLabel = null,
    IReadOnlyList<MetricPartitionSpec>? Partitions = null
) : ICommand<MetricResponse>;
