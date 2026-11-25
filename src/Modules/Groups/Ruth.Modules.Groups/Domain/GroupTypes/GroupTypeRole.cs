using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.GroupTypes;

/// <summary>Role definition owned by a <see cref="GroupType" /> governing member privileges and limits.</summary>
public sealed class GroupTypeRole : Entity<GroupTypeRoleId>
{
    private GroupTypeRole() { }

    private GroupTypeRole(GroupTypeRoleId id, GroupTypeId groupTypeId, bool isSystem)
        : base(id)
    {
        GroupTypeId = groupTypeId;
        IsSystem = isSystem;
    }

    /// <summary>Gets the owning GroupType identifier.</summary>
    public GroupTypeId GroupTypeId { get; private set; }

    /// <summary>Gets the role display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional role description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the sort order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether this is a system-managed role.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets the optional minimum member count.</summary>
    public int? MinCount { get; private set; }

    /// <summary>Gets the optional maximum active member count.</summary>
    public int? MaxCount { get; private set; }

    /// <summary>Gets whether this role represents a group leader.</summary>
    public bool IsLeader { get; private set; }

    /// <summary>Gets whether members in this role receive requirement alerts.</summary>
    public bool ReceiveRequirementsNotifications { get; private set; }

    /// <summary>Gets whether members in this role can view the group.</summary>
    public bool CanView { get; private set; } = true;

    /// <summary>Gets whether members in this role can edit the group.</summary>
    public bool CanEdit { get; private set; }

    /// <summary>Gets whether members in this role can manage group members.</summary>
    public bool CanManageMembers { get; private set; }

    /// <summary>Gets whether members in this role can record group attendance.</summary>
    public bool CanTakeAttendance { get; private set; }

    /// <summary>Gets whether members in this role are eligible for check-in.</summary>
    public bool IsCheckInAllowed { get; private set; } = true;

    /// <summary>Gets whether this role is visible on public registration screens.</summary>
    public bool IsPublic { get; private set; } = true;

    internal static Result<GroupTypeRole> Create(
        GroupTypeId groupTypeId,
        GroupTypeRoleInputDto input,
        bool isSystem = false
    )
    {
        var role = new GroupTypeRole(GroupTypeRoleId.Create(), groupTypeId, isSystem);
        var update = role.Update(input);
        return update.IsFailure
            ? Result.Failure<GroupTypeRole>(update.Errors)
            : Result.Success(role);
    }

    internal Result Update(GroupTypeRoleInputDto input)
    {
        var nameResult = GroupsTextPolicy.NormalizeRequired(input.Name);
        var descResult = GroupsTextPolicy.NormalizeOptional(input.Description);
        var combined = Result.Combine(nameResult, descResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        if (
            input.MinCount is < 0
            || input.MaxCount is < 0
            || (
                input.MinCount.HasValue
                && input.MaxCount.HasValue
                && input.MinCount > input.MaxCount
            )
        )
        {
            return GroupsErrors.InvalidRoleCounts;
        }

        Name = nameResult.Value;
        Description = descResult.Value;
        Order = input.Order;
        MinCount = input.MinCount;
        MaxCount = input.MaxCount;
        IsLeader = input.IsLeader;
        ReceiveRequirementsNotifications = input.ReceiveRequirementsNotifications;
        CanView = input.CanView;
        CanEdit = input.CanEdit;
        CanManageMembers = input.CanManageMembers;
        CanTakeAttendance = input.CanTakeAttendance;
        IsCheckInAllowed = input.IsCheckInAllowed;
        IsPublic = input.IsPublic;
        return Result.Success();
    }
}
