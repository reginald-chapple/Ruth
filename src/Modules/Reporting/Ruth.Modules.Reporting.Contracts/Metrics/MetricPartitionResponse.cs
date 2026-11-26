namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Partition dimension configured on a Metric.</summary>
/// <param name="Id">Partition identifier.</param>
/// <param name="Label">Dimension label.</param>
/// <param name="EntityTypeKey">Optional entity type key.</param>
/// <param name="IsRequired">Whether required.</param>
/// <param name="Order">Display order.</param>
public sealed record MetricPartitionResponse(
    Guid Id,
    string Label,
    string? EntityTypeKey,
    bool IsRequired,
    int Order
);
