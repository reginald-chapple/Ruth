namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Persisted MetricValue data point.</summary>
/// <param name="Id">MetricValue identifier.</param>
/// <param name="MetricId">Owning Metric identifier.</param>
/// <param name="MetricValueType">Measure or Goal.</param>
/// <param name="YValue">Numeric value.</param>
/// <param name="MetricValueDateTimeUtc">Observation timestamp.</param>
/// <param name="XValue">Optional X-axis label.</param>
/// <param name="Note">Optional note.</param>
/// <param name="Partitions">Partition dimension values.</param>
public sealed record MetricValueResponse(
    Guid Id,
    Guid MetricId,
    MetricValueType MetricValueType,
    decimal YValue,
    DateTimeOffset MetricValueDateTimeUtc,
    string? XValue,
    string? Note,
    IReadOnlyList<MetricPartitionValueInput> Partitions
);
