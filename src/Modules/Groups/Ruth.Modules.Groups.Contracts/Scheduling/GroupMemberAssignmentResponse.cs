namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Standing assignment preference mapping a GroupMember to a Location and/or Schedule.</summary>
public sealed record GroupMemberAssignmentResponse(
    Guid Id,
    Guid GroupId,
    Guid GroupMemberId,
    Guid PersonId,
    Guid? LocationId,
    Guid? ScheduleId,
    DateTimeOffset? ConfirmationSentOnUtc,
    DateTimeOffset? LastReminderSentOnUtc
);
