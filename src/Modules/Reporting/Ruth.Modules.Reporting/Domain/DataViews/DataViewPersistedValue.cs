using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Materialized entity identifier row persisted for fast DataView reuse and set intersections.</summary>
public sealed class DataViewPersistedValue : ITenantScoped
{
    private DataViewPersistedValue() { }

    public DataViewPersistedValue(
        DataViewId dataViewId,
        Guid entityId,
        DateTimeOffset persistedDateTimeUtc
    )
    {
        DataViewId = dataViewId;
        EntityId = entityId;
        PersistedDateTimeUtc = persistedDateTimeUtc.ToUniversalTime();
    }

    public TenantId TenantId { get; private set; }
    public DataViewId DataViewId { get; private set; }
    public Guid EntityId { get; private set; }
    public DateTimeOffset PersistedDateTimeUtc { get; private set; }

    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign {nameof(DataViewPersistedValue)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
