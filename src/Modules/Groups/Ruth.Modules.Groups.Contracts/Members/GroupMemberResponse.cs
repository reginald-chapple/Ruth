namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Detailed projection of a GroupMember including role, status, and soft capacity warnings.</summary>
public sealed record GroupMemberResponse(
    Guid Id,
    Guid GroupId,
    string GroupName,
    Guid GroupTypeId,
    Guid PersonId,
    Guid GroupRoleId,
    string GroupRoleName,
    bool IsLeader,
    GroupMemberStatus Status,
    string? Note,
    int? GuestCount,
    DateTimeOffset? DateTimeAddedUtc,
    DateTimeOffset? InactiveDateTimeUtc,
    bool IsArchived,
    DateTimeOffset? ArchivedDateTimeUtc,
    Guid? ArchivedByPersonId,
    int? GroupOrder,
    bool IsNotified,
    Guid? ScheduleTemplateId,
    DateOnly? ScheduleStartDate,
    int? ScheduleReminderEmailOffsetDays,
    GroupCommunicationPreference CommunicationPreference,
    IReadOnlyDictionary<string, string?> Attributes,
    string? CapacityWarning,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
