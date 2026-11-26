namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Input specification defining a partition dimension on a Metric (e.g., Branch, Program, Fund).</summary>
/// <param name="Label">Human-readable partition label.</param>
/// <param name="EntityTypeKey">Optional entity type key backing the partition.</param>
/// <param name="IsRequired">Whether a partition entity value is required on each MetricValue.</param>
public sealed record MetricPartitionSpec(
    string Label,
    string? EntityTypeKey = null,
    bool IsRequired = false
);
