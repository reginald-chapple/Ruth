using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain.GroupTypes;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal static class GroupTypeMapper
{
    public static GroupTypeSummaryResponse ToSummary(GroupType groupType)
    {
        return new GroupTypeSummaryResponse(
            groupType.Id.Value,
            groupType.Name,
            groupType.Description,
            groupType.GroupTerm,
            groupType.GroupMemberTerm,
            groupType.IconCssClass,
            groupType.Order,
            groupType.IsSystem,
            groupType.ShowInGroupList,
            groupType.ShowInNavigation,
            groupType.TakesAttendance,
            groupType.CountsTowardHeadlineAttendance,
            groupType.IsSchedulingEnabled,
            groupType.GroupCapacityRule,
            groupType.Roles.Count
        );
    }

    public static GroupTypeDetailResponse ToDetail(GroupType groupType)
    {
        GroupTypeRoleResponse[] roles =
        [
            .. groupType
                .Roles.OrderBy(static r => r.Order)
                .ThenBy(static r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToRole),
        ];
        Guid[] childIds =
        [
            .. groupType.AllowedChildren.Select(static c => c.ChildGroupTypeId.Value),
        ];

        return new GroupTypeDetailResponse(
            groupType.Id.Value,
            groupType.Name,
            groupType.Description,
            groupType.GroupTerm,
            groupType.GroupMemberTerm,
            groupType.IconCssClass,
            groupType.Order,
            groupType.IsSystem,
            groupType.ShowInGroupList,
            groupType.ShowInNavigation,
            groupType.AllowMultipleLocations,
            groupType.IsMeetingStyleEnabled,
            groupType.GroupsRequireBranch,
            groupType.EnableGroupHistory,
            groupType.TakesAttendance,
            groupType.CountsTowardHeadlineAttendance,
            groupType.SendAttendanceReminder,
            groupType.GroupAttendanceRequiresLocation,
            groupType.GroupAttendanceRequiresSchedule,
            groupType.AttendanceRule,
            groupType.GroupCapacityRule,
            groupType.LocationSelectionMode,
            groupType.EnableLocationSchedules,
            groupType.IsSchedulingEnabled,
            groupType.ScheduleConfirmationLogic,
            groupType.ScheduleReminderEmailOffsetDays,
            groupType.PurposeValueId,
            groupType.DefaultGroupRoleId?.Value,
            groupType.InheritedGroupTypeId?.Value,
            roles,
            childIds,
            new Dictionary<string, string?>(
                groupType.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            groupType.CreatedOnUtc,
            groupType.ModifiedOnUtc
        );
    }

    public static GroupTypeRoleResponse ToRole(GroupTypeRole role)
    {
        return new GroupTypeRoleResponse(
            role.Id.Value,
            role.GroupTypeId.Value,
            role.Name,
            role.Description,
            role.Order,
            role.IsSystem,
            role.MinCount,
            role.MaxCount,
            role.IsLeader,
            role.ReceiveRequirementsNotifications,
            role.CanView,
            role.CanEdit,
            role.CanManageMembers,
            role.CanTakeAttendance,
            role.IsCheckInAllowed,
            role.IsPublic
        );
    }
}
