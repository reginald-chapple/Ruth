using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Updates properties on an existing Group.</summary>
public sealed record UpdateGroupCommand(
    Guid GroupId,
    string Name,
    Guid? ParentGroupId = null,
    Guid? CampusId = null,
    Guid? ScheduleId = null,
    string? Description = null,
    Guid? PhotoId = null,
    MeetingStyle? MeetingStyle = null,
    bool IsActive = true,
    bool IsPublic = true,
    int Order = 0,
    bool AllowGuests = false,
    int? GroupCapacity = null,
    Guid? StatusValueId = null,
    Guid? RequiredSignatureDocumentTemplateId = null,
    bool SchedulingMustMeetRequirements = false,
    Guid? ScheduleCoordinatorPersonId = null,
    Guid? GroupAdministratorPersonId = null,
    bool DisableScheduling = false,
    bool DisableScheduleToolboxAccess = false,
    int? RsvpReminderOffsetDays = null
) : ICommand<GroupDetailResponse>;
