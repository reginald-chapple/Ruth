using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Updates properties and allowed child group types on an existing GroupType.</summary>
public sealed record UpdateGroupTypeCommand(
    Guid GroupTypeId,
    string Name,
    string? Description = null,
    string GroupTerm = "Group",
    string GroupMemberTerm = "Member",
    string? IconCssClass = null,
    int Order = 0,
    bool ShowInGroupList = true,
    bool ShowInNavigation = true,
    bool AllowMultipleLocations = false,
    bool IsMeetingStyleEnabled = false,
    bool GroupsRequireBranch = false,
    bool EnableGroupHistory = false,
    bool TakesAttendance = false,
    bool CountsTowardHeadlineAttendance = false,
    bool SendAttendanceReminder = false,
    bool GroupAttendanceRequiresLocation = false,
    bool GroupAttendanceRequiresSchedule = false,
    AttendanceRule AttendanceRule = AttendanceRule.None,
    GroupCapacityRule GroupCapacityRule = GroupCapacityRule.None,
    GroupLocationPickerMode LocationSelectionMode = GroupLocationPickerMode.Named,
    bool EnableLocationSchedules = false,
    bool IsSchedulingEnabled = false,
    ScheduleConfirmationLogic ScheduleConfirmationLogic = ScheduleConfirmationLogic.Ask,
    int? ScheduleReminderEmailOffsetDays = null,
    Guid? PurposeValueId = null,
    Guid? DefaultGroupRoleId = null,
    Guid? InheritedGroupTypeId = null,
    IReadOnlyList<Guid>? AllowedChildGroupTypeIds = null
) : ICommand<GroupTypeDetailResponse>;
