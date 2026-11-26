using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema daily operational metric fact table (reporting.fact_metric_daily).</summary>
public sealed class FactMetricDaily : ITenantScoped
{
    public TenantId TenantId { get; private set; }
    public Guid Id { get; set; }
    public int DateKey { get; set; }
    public Guid MetricId { get; set; }
    public Guid? PartitionEntityId { get; set; }
    public string PartitionLabel { get; set; } = "All";
    public decimal MeasureSum { get; set; }
    public decimal? GoalSum { get; set; }
    public int SampleCount { get; set; }
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
                $"Cannot reassign {nameof(FactMetricDaily)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
