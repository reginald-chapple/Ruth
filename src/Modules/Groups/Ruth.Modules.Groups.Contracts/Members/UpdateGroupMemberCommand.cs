using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Updates a GroupMember's role, status, notes, scheduling preferences, or attributes.</summary>
public sealed record UpdateGroupMemberCommand(
    Guid GroupMemberId,
    Guid GroupRoleId,
    GroupMemberStatus Status,
    string? Note = null,
    int? GuestCount = null,
    int? GroupOrder = null,
    Guid? ScheduleTemplateId = null,
    DateOnly? ScheduleStartDate = null,
    int? ScheduleReminderEmailOffsetDays = null,
    GroupCommunicationPreference CommunicationPreference =
        GroupCommunicationPreference.RecipientPreference,
    bool SkipRequirementsCheck = false
) : ICommand<GroupMemberResponse>;
