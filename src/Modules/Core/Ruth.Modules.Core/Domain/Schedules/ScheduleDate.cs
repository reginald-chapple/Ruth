namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Materialized occurrence row for fast SQL range joins against active schedules.</summary>
public sealed class ScheduleDate
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

    /// <summary>Gets the owning schedule identifier (part 1 of composite PK).</summary>
    public ScheduleId ScheduleId { get; private set; }

    /// <summary>Gets the occurrence UTC start instant (part 2 of composite PK).</summary>
    public DateTimeOffset StartOnUtc { get; private set; }

    /// <summary>Gets the occurrence UTC end instant.</summary>
    public DateTimeOffset EndOnUtc { get; private set; }

    /// <summary>Gets the integer YYYYMMDD local date key for fast analytics joins.</summary>
    public int StartDateKey { get; private set; }
}
