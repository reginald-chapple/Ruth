namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Materialized entity identifier row persisted for fast DataView reuse and set intersections.</summary>
public sealed class DataViewPersistedValue
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

    public DataViewId DataViewId { get; private set; }
    public Guid EntityId { get; private set; }
    public DateTimeOffset PersistedDateTimeUtc { get; private set; }
}
