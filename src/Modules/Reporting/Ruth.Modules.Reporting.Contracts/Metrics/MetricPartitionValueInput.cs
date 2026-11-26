namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Partition dimension assignment on a single MetricValue entry.</summary>
/// <param name="MetricPartitionId">Target MetricPartition identifier.</param>
/// <param name="EntityId">Optional entity identifier (e.g., BranchId).</param>
/// <param name="EntityLabel">Optional human-readable label for chart grouping.</param>
public sealed record MetricPartitionValueInput(
    Guid MetricPartitionId,
    Guid? EntityId = null,
    string? EntityLabel = null
);
