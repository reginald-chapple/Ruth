using System.Globalization;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Members;

/// <summary>Pure domain policy evaluating role limits, hard/soft capacity, and duplicate membership rules.</summary>
internal static class GroupMembershipPolicy
{
    /// <summary>Evaluates whether adding or activating a member violates role or group capacity limits.</summary>
    public static Result<string?> EvaluateCapacityAndRoleLimits(
        Group group,
        GroupType groupType,
        GroupTypeRole role,
        GroupMemberStatus targetStatus,
        int existingActiveInRoleExcludingSelf,
        int existingActiveUnitsInGroupExcludingSelf,
        int? targetGuestCount
    )
    {
        if (targetStatus != GroupMemberStatus.Active)
        {
            return Result.Success<string?>(value: null);
        }

        if (role.MaxCount.HasValue && existingActiveInRoleExcludingSelf + 1 > role.MaxCount.Value)
        {
            return GroupsErrors.RoleMaxCountExceeded;
        }

        if (!group.GroupCapacity.HasValue || groupType.GroupCapacityRule == GroupCapacityRule.None)
        {
            return Result.Success<string?>(value: null);
        }

        var guestUnits = group.AllowGuests && targetGuestCount is > 0 ? targetGuestCount.Value : 0;
        var projectedTotal = existingActiveUnitsInGroupExcludingSelf + 1 + guestUnits;
        if (projectedTotal <= group.GroupCapacity.Value)
        {
            return Result.Success<string?>(value: null);
        }

        var overage = projectedTotal - group.GroupCapacity.Value;
        return groupType.GroupCapacityRule == GroupCapacityRule.Hard
            ? (Result<string?>)GroupsErrors.GroupHardCapacityExceeded
            : Result.Success<string?>(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Group '{group.Name}' exceeds soft capacity ({group.GroupCapacity.Value}) by {overage}."
                )
            );
    }

    /// <summary>Computes total headcount units occupied by a list of active members (including guests when enabled).</summary>
    public static int ComputeActiveOccupancyUnits(
        IEnumerable<GroupMember> members,
        bool allowGuests,
        GroupMemberId? excludeMemberId = null
    )
    {
        var total = 0;
        foreach (var member in members)
        {
            if (member.IsArchived || member.Status != GroupMemberStatus.Active)
            {
                continue;
            }

            if (excludeMemberId.HasValue && member.Id == excludeMemberId.Value)
            {
                continue;
            }

            total += 1 + (allowGuests && member.GuestCount is > 0 ? member.GuestCount.Value : 0);
        }

        return total;
    }
}
