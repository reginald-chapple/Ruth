using Ruth.Modules.Groups.Contracts.GroupTypes;

namespace Ruth.Modules.Groups.Domain.GroupTypes;

/// <summary>Internal parameter bundle for creating or updating a <see cref="GroupType" />.</summary>
internal sealed record GroupTypeInput(
    string Name,
    string? Description,
    string GroupTerm,
    string GroupMemberTerm,
    string? IconCssClass,
    int Order,
    bool ShowInGroupList,
    bool ShowInNavigation,
    bool AllowMultipleLocations,
    bool IsMeetingStyleEnabled,
    bool GroupsRequireBranch,
    bool EnableGroupHistory,
    bool TakesAttendance,
    bool CountsTowardHeadlineAttendance,
    bool SendAttendanceReminder,
    bool GroupAttendanceRequiresLocation,
    bool GroupAttendanceRequiresSchedule,
    AttendanceRule AttendanceRule,
    GroupCapacityRule GroupCapacityRule,
    GroupLocationPickerMode LocationSelectionMode,
    bool EnableLocationSchedules,
    bool IsSchedulingEnabled,
    ScheduleConfirmationLogic ScheduleConfirmationLogic,
    int? ScheduleReminderEmailOffsetDays,
    Guid? PurposeValueId,
    GroupTypeId? InheritedGroupTypeId
);
