namespace Ruth.Modules.Reporting.Domain.Metrics;

/// <summary>Partition entity assignment belonging to a MetricValue.</summary>
public sealed class MetricValuePartition
{
    private MetricValuePartition() { }

    public MetricValuePartition(
        MetricValueId metricValueId,
        MetricPartitionId metricPartitionId,
        Guid? entityId,
        string? entityLabel
    )
    {
        MetricValueId = metricValueId;
        MetricPartitionId = metricPartitionId;
        EntityId = entityId;
        EntityLabel = string.IsNullOrWhiteSpace(entityLabel) ? null : entityLabel.Trim();
    }

    public MetricValueId MetricValueId { get; private set; }
    public MetricPartitionId MetricPartitionId { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? EntityLabel { get; private set; }
}
