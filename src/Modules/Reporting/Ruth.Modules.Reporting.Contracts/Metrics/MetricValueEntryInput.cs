namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Single manual or calculated metric value input item.</summary>
/// <param name="MetricId">Target Metric identifier.</param>
/// <param name="MetricValueType">Measure or Goal.</param>
/// <param name="YValue">Numeric metric value.</param>
/// <param name="MetricValueDateTimeUtc">Timestamp for the metric value.</param>
/// <param name="XValue">Optional X-axis label.</param>
/// <param name="Note">Optional operator note.</param>
/// <param name="Partitions">Optional partition dimension values.</param>
public sealed record MetricValueEntryInput(
    Guid MetricId,
    MetricValueType MetricValueType,
    decimal YValue,
    DateTimeOffset MetricValueDateTimeUtc,
    string? XValue = null,
    string? Note = null,
    IReadOnlyList<MetricPartitionValueInput>? Partitions = null
);
