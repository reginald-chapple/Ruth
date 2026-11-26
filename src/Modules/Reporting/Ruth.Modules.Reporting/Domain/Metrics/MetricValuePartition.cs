using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.Metrics;

/// <summary>Partition entity assignment belonging to a MetricValue.</summary>
public sealed class MetricValuePartition : ITenantScoped
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

    public TenantId TenantId { get; private set; }
    public MetricValueId MetricValueId { get; private set; }
    public MetricPartitionId MetricPartitionId { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? EntityLabel { get; private set; }

    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign {nameof(MetricValuePartition)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
