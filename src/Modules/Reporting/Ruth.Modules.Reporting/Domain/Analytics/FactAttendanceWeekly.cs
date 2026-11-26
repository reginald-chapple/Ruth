using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema weekly attendance fact table (reporting.fact_attendance_weekly).</summary>
public sealed class FactAttendanceWeekly : ITenantScoped
{
    public TenantId TenantId { get; private set; }
    public Guid Id { get; set; }
    public int WeekEndDateKey { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public Guid? BranchId { get; set; }
    public int OccurrenceCount { get; set; }
    public int AttendedCount { get; set; }
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
                $"Cannot reassign {nameof(FactAttendanceWeekly)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
