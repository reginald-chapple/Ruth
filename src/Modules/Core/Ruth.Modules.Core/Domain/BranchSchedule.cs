using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Links a <see cref="Branch" /> to an ordered <see cref="Schedules.Schedule" />.</summary>
public sealed class BranchSchedule : Entity<Guid>
{
    private BranchSchedule() { }

    internal BranchSchedule(
        Guid id,
        BranchId branchId,
        Guid scheduleId,
        Guid? scheduleTypeValueId,
        int order
    )
        : base(id)
    {
        BranchId = branchId;
        ScheduleId = scheduleId;
        ScheduleTypeValueId = scheduleTypeValueId;
        Order = Math.Max(0, order);
    }

    /// <summary>Gets the owning branch identifier.</summary>
    public BranchId BranchId { get; private set; }

    /// <summary>Gets the linked schedule identifier.</summary>
    public Guid ScheduleId { get; private set; }

    /// <summary>Gets the optional schedule type defined value identifier.</summary>
    public Guid? ScheduleTypeValueId { get; private set; }

    /// <summary>Gets the display order within the branch.</summary>
    public int Order { get; private set; }
}
