using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Adds a person to a group with capacity, role-limit, duplicate, and requirement validation.</summary>
public sealed record AddGroupMemberCommand(
    Guid GroupId,
    Guid PersonId,
    Guid? GroupRoleId = null,
    GroupMemberStatus Status = GroupMemberStatus.Active,
    string? Note = null,
    int? GuestCount = null,
    int? GroupOrder = null,
    Guid? ScheduleTemplateId = null,
    DateOnly? ScheduleStartDate = null,
    int? ScheduleReminderEmailOffsetDays = null,
    GroupCommunicationPreference CommunicationPreference =
        GroupCommunicationPreference.RecipientPreference,
    bool SkipRequirementsCheck = false,
    IReadOnlyDictionary<string, string?>? Attributes = null
) : ICommand<GroupMemberResponse>;
