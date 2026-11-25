using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Creates a new GroupType with optional initial roles and allowed child group types.</summary>
public sealed record CreateGroupTypeCommand(
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
    bool GroupsRequireCampus = false,
    bool EnableGroupHistory = false,
    bool TakesAttendance = false,
    bool AttendanceCountsAsWeekendService = false,
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
    Guid? InheritedGroupTypeId = null,
    IReadOnlyList<GroupTypeRoleInputDto>? InitialRoles = null,
    IReadOnlyList<Guid>? AllowedChildGroupTypeIds = null
) : ICommand<GroupTypeDetailResponse>;
