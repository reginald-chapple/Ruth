namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Summary projection of a GroupType.</summary>
public sealed record GroupTypeSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    string GroupTerm,
    string GroupMemberTerm,
    string? IconCssClass,
    int Order,
    bool IsSystem,
    bool ShowInGroupList,
    bool ShowInNavigation,
    bool TakesAttendance,
    bool AttendanceCountsAsWeekendService,
    bool IsSchedulingEnabled,
    GroupCapacityRule GroupCapacityRule,
    int RoleCount
);
