using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Standing serving location and/or schedule assignment preference for a GroupMember.</summary>
public sealed class GroupMemberAssignment : AggregateRoot<GroupMemberAssignmentId>, IAuditableEntity
{
    private GroupMemberAssignment() { }

    private GroupMemberAssignment(
        GroupMemberAssignmentId id,
        GroupId groupId,
        GroupMemberId groupMemberId,
        Guid? locationId,
        Guid? scheduleId
    )
        : base(id)
    {
        GroupId = groupId;
        GroupMemberId = groupMemberId;
        LocationId = locationId;
        ScheduleId = scheduleId;
    }

    /// <summary>Gets the Group identifier.</summary>
    public GroupId GroupId { get; private set; }

    /// <summary>Gets the GroupMember identifier.</summary>
    public GroupMemberId GroupMemberId { get; private set; }

    /// <summary>Gets the optional Core Location identifier.</summary>
    public Guid? LocationId { get; private set; }

    /// <summary>Gets the optional Core Schedule identifier.</summary>
    public Guid? ScheduleId { get; private set; }

    /// <summary>Gets when confirmation was last sent.</summary>
    public DateTimeOffset? ConfirmationSentOnUtc { get; private set; }

    /// <summary>Gets when a reminder was last sent.</summary>
    public DateTimeOffset? LastReminderSentOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    internal static GroupMemberAssignment Create(
        GroupId groupId,
        GroupMemberId groupMemberId,
        Guid? locationId,
        Guid? scheduleId
    )
    {
        return new GroupMemberAssignment(
            GroupMemberAssignmentId.Create(),
            groupId,
            groupMemberId,
            locationId,
            scheduleId
        );
    }

    internal void MarkConfirmationSent(DateTimeOffset sentOnUtc)
    {
        ConfirmationSentOnUtc = sentOnUtc.ToUniversalTime();
    }

    internal void MarkReminderSent(DateTimeOffset sentOnUtc)
    {
        LastReminderSentOnUtc = sentOnUtc.ToUniversalTime();
    }
}
