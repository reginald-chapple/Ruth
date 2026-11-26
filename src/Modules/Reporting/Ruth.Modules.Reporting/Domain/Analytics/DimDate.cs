using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema calendar date dimension table (reporting.dim_date).</summary>
public sealed class DimDate : ITenantScoped
{
    public TenantId TenantId { get; private set; }
    public int DateKey { get; set; }
    public DateOnly CalendarDate { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public int IsoWeek { get; set; }
    public int DayOfWeek { get; set; }

    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign {nameof(DimDate)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
