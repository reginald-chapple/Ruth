namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Point-in-time historical snapshot of a GroupMember.</summary>
public sealed record GroupMemberHistoricalResponse(
    Guid Id,
    Guid GroupMemberId,
    Guid GroupId,
    Guid PersonId,
    Guid GroupRoleId,
    string GroupRoleName,
    bool IsLeader,
    GroupMemberStatus Status,
    bool IsArchived,
    DateTimeOffset? ArchivedDateTimeUtc,
    Guid? ArchivedByPersonId,
    DateTimeOffset? InactiveDateTimeUtc,
    DateTimeOffset EffectiveDateTimeUtc,
    DateTimeOffset ExpireDateTimeUtc,
    bool CurrentRowIndicator
);
