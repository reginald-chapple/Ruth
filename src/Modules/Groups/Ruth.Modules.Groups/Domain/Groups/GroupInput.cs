using Ruth.Modules.Groups.Contracts.Groups;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Internal parameter bundle for creating or updating a <see cref="Group" />.</summary>
internal sealed record GroupInput(
    string Name,
    GroupId? ParentGroupId,
    Guid? CampusId,
    Guid? ScheduleId,
    string? Description,
    Guid? PhotoId,
    MeetingStyle? MeetingStyle,
    bool IsActive,
    bool IsPublic,
    int Order,
    bool AllowGuests,
    int? GroupCapacity,
    Guid? StatusValueId,
    Guid? RequiredSignatureDocumentTemplateId,
    bool SchedulingMustMeetRequirements,
    Guid? ScheduleCoordinatorPersonId,
    Guid? GroupAdministratorPersonId,
    bool DisableScheduling,
    bool DisableScheduleToolboxAccess,
    int? RsvpReminderOffsetDays
);
