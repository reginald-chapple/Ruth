using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema Metric catalog dimension table (reporting.dim_metric).</summary>
public sealed class DimMetric : ITenantScoped
{
    public TenantId TenantId { get; private set; }
    public Guid MetricId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? UnitLabel { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public DateTimeOffset RefreshedAtUtc { get; set; }

    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign {nameof(DimMetric)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
