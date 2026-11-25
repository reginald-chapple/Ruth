using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.GroupTypes;

public sealed partial class GroupType
{
    /// <summary>Returns true if a group of <paramref name="childGroupTypeId" /> may be nested under this type.</summary>
    public bool AllowsChildGroupType(GroupTypeId childGroupTypeId)
    {
        return _allowedChildren.Exists(c => c.ChildGroupTypeId == childGroupTypeId);
    }

    internal static Result<GroupType> Create(
        GroupTypeInput input,
        IReadOnlyList<GroupTypeRoleInputDto>? initialRoles = null,
        IReadOnlyList<GroupTypeId>? allowedChildIds = null,
        bool isSystem = false
    )
    {
        var groupType = new GroupType(GroupTypeId.Create(), isSystem);
        var apply = groupType.ApplyInput(input);
        if (apply.IsFailure)
        {
            return Result.Failure<GroupType>(apply.Errors);
        }

        var roleInputs = initialRoles is { Count: > 0 }
            ? initialRoles
            : [new GroupTypeRoleInputDto("Member", IsPublic: true)];

        foreach (var roleInput in roleInputs)
        {
            var added = groupType.AddRole(
                roleInput,
                setAsDefault: !groupType.DefaultGroupRoleId.HasValue
            );
            if (added.IsFailure)
            {
                return Result.Failure<GroupType>(added.Errors);
            }
        }

        groupType.SetAllowedChildGroupTypes(allowedChildIds ?? [groupType.Id]);
        return Result.Success(groupType);
    }

    internal Result Update(
        GroupTypeInput input,
        GroupTypeRoleId? defaultGroupRoleId,
        IReadOnlyList<GroupTypeId>? allowedChildIds
    )
    {
        var apply = ApplyInput(input);
        if (apply.IsFailure)
        {
            return apply;
        }

        if (defaultGroupRoleId.HasValue)
        {
            if (_roles.TrueForAll(r => r.Id != defaultGroupRoleId.Value))
            {
                return GroupsErrors.GroupTypeRoleNotFound;
            }

            DefaultGroupRoleId = defaultGroupRoleId;
        }

        if (allowedChildIds is not null)
        {
            SetAllowedChildGroupTypes(allowedChildIds);
        }

        return Result.Success();
    }

    internal Result<GroupTypeRole> AddRole(
        GroupTypeRoleInputDto input,
        bool setAsDefault = false,
        bool isSystem = false
    )
    {
        var created = GroupTypeRole.Create(Id, input, isSystem);
        if (created.IsFailure)
        {
            return created;
        }

        if (
            _roles.Exists(r =>
                string.Equals(r.Name, created.Value.Name, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return GroupsErrors.DuplicateRoleName;
        }

        _roles.Add(created.Value);
        if (setAsDefault || !DefaultGroupRoleId.HasValue)
        {
            DefaultGroupRoleId = created.Value.Id;
        }

        return created;
    }

    internal Result UpdateRole(GroupTypeRoleId roleId, GroupTypeRoleInputDto input)
    {
        var role = _roles.FirstOrDefault(r => r.Id == roleId);
        if (role is null)
        {
            return GroupsErrors.GroupTypeRoleNotFound;
        }

        var trimmed = input.Name?.Trim() ?? string.Empty;
        return _roles.Exists(r =>
            r.Id != roleId && string.Equals(r.Name, trimmed, StringComparison.OrdinalIgnoreCase)
        )
            ? (Result)GroupsErrors.DuplicateRoleName
            : role.Update(input);
    }

    internal Result RemoveRole(GroupTypeRoleId roleId)
    {
        var role = _roles.FirstOrDefault(r => r.Id == roleId);
        if (role is null)
        {
            return GroupsErrors.GroupTypeRoleNotFound;
        }

        if (DefaultGroupRoleId == roleId)
        {
            return GroupsErrors.RoleInUse;
        }

        _ = _roles.Remove(role);
        return Result.Success();
    }

    internal void SetAllowedChildGroupTypes(IEnumerable<GroupTypeId> childIds)
    {
        _allowedChildren.Clear();
        foreach (var childId in childIds.Distinct())
        {
            _allowedChildren.Add(new GroupTypeAllowedChild(Id, childId));
        }
    }

    private Result ApplyInput(GroupTypeInput input)
    {
        var nameResult = GroupsTextPolicy.NormalizeRequired(input.Name);
        var descResult = GroupsTextPolicy.NormalizeOptional(input.Description);
        var groupTermResult = GroupsTextPolicy.NormalizeRequired(
            string.IsNullOrWhiteSpace(input.GroupTerm) ? "Group" : input.GroupTerm
        );
        var memberTermResult = GroupsTextPolicy.NormalizeRequired(
            string.IsNullOrWhiteSpace(input.GroupMemberTerm) ? "Member" : input.GroupMemberTerm
        );
        var iconResult = GroupsTextPolicy.NormalizeOptional(
            input.IconCssClass,
            GroupsTextPolicy.NameMaxLength,
            allowNewLines: false
        );

        var combined = Result.Combine(
            nameResult,
            descResult,
            groupTermResult,
            memberTermResult,
            iconResult
        );
        if (combined.IsFailure)
        {
            return combined;
        }

        Name = nameResult.Value;
        Description = descResult.Value;
        GroupTerm = groupTermResult.Value;
        GroupMemberTerm = memberTermResult.Value;
        IconCssClass = iconResult.Value;
        Order = input.Order;
        ShowInGroupList = input.ShowInGroupList;
        ShowInNavigation = input.ShowInNavigation;
        AllowMultipleLocations = input.AllowMultipleLocations;
        IsMeetingStyleEnabled = input.IsMeetingStyleEnabled;
        GroupsRequireCampus = input.GroupsRequireCampus;
        EnableGroupHistory = input.EnableGroupHistory;
        TakesAttendance = input.TakesAttendance;
        AttendanceCountsAsWeekendService = input.AttendanceCountsAsWeekendService;
        SendAttendanceReminder = input.SendAttendanceReminder;
        GroupAttendanceRequiresLocation = input.GroupAttendanceRequiresLocation;
        GroupAttendanceRequiresSchedule = input.GroupAttendanceRequiresSchedule;
        AttendanceRule = input.AttendanceRule;
        GroupCapacityRule = input.GroupCapacityRule;
        LocationSelectionMode = input.LocationSelectionMode;
        EnableLocationSchedules = input.EnableLocationSchedules;
        IsSchedulingEnabled = input.IsSchedulingEnabled;
        ScheduleConfirmationLogic = input.ScheduleConfirmationLogic;
        ScheduleReminderEmailOffsetDays = input.ScheduleReminderEmailOffsetDays;
        PurposeValueId = input.PurposeValueId;
        InheritedGroupTypeId = input.InheritedGroupTypeId == Id ? null : input.InheritedGroupTypeId;
        return Result.Success();
    }
}
