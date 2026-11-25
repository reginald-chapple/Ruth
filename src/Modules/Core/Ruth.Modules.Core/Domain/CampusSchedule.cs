using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Links a <see cref="Campus" /> to an ordered <see cref="Schedules.Schedule" />.</summary>
public sealed class CampusSchedule : Entity<Guid>
{
    private CampusSchedule() { }

    internal CampusSchedule(
        Guid id,
        CampusId campusId,
        Guid scheduleId,
        Guid? scheduleTypeValueId,
        int order
    )
        : base(id)
    {
        CampusId = campusId;
        ScheduleId = scheduleId;
        ScheduleTypeValueId = scheduleTypeValueId;
        Order = Math.Max(0, order);
    }

    /// <summary>Gets the owning campus identifier.</summary>
    public CampusId CampusId { get; private set; }

    /// <summary>Gets the linked schedule identifier.</summary>
    public Guid ScheduleId { get; private set; }

    /// <summary>Gets the optional schedule type defined value identifier.</summary>
    public Guid? ScheduleTypeValueId { get; private set; }

    /// <summary>Gets the display order within the campus.</summary>
    public int Order { get; private set; }
}
