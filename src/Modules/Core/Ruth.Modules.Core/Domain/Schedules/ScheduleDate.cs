using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Materialized occurrence row for fast SQL range joins against active schedules.</summary>
public sealed class ScheduleDate : ITenantScoped
{
    private ScheduleDate() { }

    internal ScheduleDate(
        ScheduleId scheduleId,
        DateTimeOffset startOnUtc,
        DateTimeOffset endOnUtc,
        int startDateKey
    )
    {
        ScheduleId = scheduleId;
        StartOnUtc = startOnUtc;
        EndOnUtc = endOnUtc;
        StartDateKey = startDateKey;
    }

    /// <inheritdoc />
    public TenantId TenantId { get; private set; }

    /// <summary>Gets the owning schedule identifier (part 1 of composite PK).</summary>
    public ScheduleId ScheduleId { get; private set; }

    /// <summary>Gets the occurrence UTC start instant (part 2 of composite PK).</summary>
    public DateTimeOffset StartOnUtc { get; private set; }

    /// <summary>Gets the occurrence UTC end instant.</summary>
    public DateTimeOffset EndOnUtc { get; private set; }

    /// <summary>Gets the integer YYYYMMDD local date key for fast analytics joins.</summary>
    public int StartDateKey { get; private set; }

    /// <inheritdoc />
    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign ScheduleDate ({ScheduleId}) from tenant '{TenantId}' to '{tenantId}'."
            );
        }

        TenantId = tenantId;
    }
}
