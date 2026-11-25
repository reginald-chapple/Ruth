namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Point-in-time historical snapshot of a Group.</summary>
public sealed record GroupHistoricalResponse(
    Guid Id,
    Guid GroupId,
    string GroupName,
    Guid GroupTypeId,
    string GroupTypeName,
    Guid? CampusId,
    Guid? ParentGroupId,
    string? Description,
    Guid? StatusValueId,
    Guid? ScheduleId,
    bool IsActive,
    bool IsArchived,
    DateTimeOffset? InactiveDateTimeUtc,
    DateTimeOffset? ArchivedDateTimeUtc,
    Guid? ArchivedByPersonId,
    DateTimeOffset EffectiveDateTimeUtc,
    DateTimeOffset ExpireDateTimeUtc,
    bool CurrentRowIndicator
);
