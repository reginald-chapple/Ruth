using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.Modules.Groups.Slices.Requirements;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class GroupMemberAddService(
    GroupsDbContext dbContext,
    GroupRequirementEvaluatorService requirementEvaluator,
    IClock clock
)
{
    public async Task<Result<GroupMemberResponse>> AddMemberAsync(
        AddGroupMemberCommand command,
        CancellationToken cancellationToken
    )
    {
        var loaded = await LoadGroupContextAsync(GroupId.From(command.GroupId), cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<GroupMemberResponse>(loaded.Errors);
        }

        var (group, groupType) = loaded.Value;
        var roleResult = ResolveRole(groupType, command.GroupRoleId);
        if (roleResult.IsFailure)
        {
            return Result.Failure<GroupMemberResponse>(roleResult.Errors);
        }

        var role = roleResult.Value;
        var validation = await ValidateEligibilityAsync(
            group,
            groupType,
            role,
            command,
            cancellationToken
        );
        return validation.IsFailure
            ? Result.Failure<GroupMemberResponse>(validation.Errors)
            : await PersistMemberAsync(
                group,
                groupType,
                role,
                command,
                validation.Value,
                cancellationToken
            );
    }

    private async Task<Result<(Group Group, GroupType GroupType)>> LoadGroupContextAsync(
        GroupId groupId,
        CancellationToken cancellationToken
    )
    {
        var group = await dbContext.Groups.FirstOrDefaultAsync(
            g => g.Id == groupId,
            cancellationToken
        );
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.Roles)
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        return groupType is null
            ? (Result<(Group, GroupType)>)GroupsErrors.GroupTypeNotFound
            : Result.Success((group, groupType));
    }

    private async Task<Result<string?>> ValidateEligibilityAsync(
        Group group,
        GroupType groupType,
        GroupTypeRole role,
        AddGroupMemberCommand command,
        CancellationToken cancellationToken
    )
    {
        var existingMembers = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m => m.GroupId == group.Id && !m.IsArchived)
            .ToListAsync(cancellationToken);

        if (existingMembers.Exists(m => m.PersonId == command.PersonId && m.GroupRoleId == role.Id))
        {
            return GroupsErrors.DuplicateMemberRole;
        }

        var capacityCheck = EvaluateLimits(group, groupType, role, existingMembers, command);
        if (capacityCheck.IsFailure || command.SkipRequirementsCheck)
        {
            return capacityCheck;
        }

        var meets = await requirementEvaluator.CheckMustMeetRequirementsForAddAsync(
            group,
            command.PersonId,
            role.Id,
            cancellationToken
        );
        return meets ? capacityCheck : (Result<string?>)GroupsErrors.RequirementsNotMetForAdd;
    }

    private async Task<Result<GroupMemberResponse>> PersistMemberAsync(
        Group group,
        GroupType groupType,
        GroupTypeRole role,
        AddGroupMemberCommand command,
        string? capacityWarning,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var templateId = command.ScheduleTemplateId.HasValue
            ? GroupMemberScheduleTemplateId.From(command.ScheduleTemplateId.Value)
            : (GroupMemberScheduleTemplateId?)null;

        var created = GroupMember.Create(
            group,
            command.PersonId,
            role.Id,
            command.Status,
            command.Note,
            command.GuestCount,
            command.GroupOrder,
            templateId,
            command.ScheduleStartDate,
            command.ScheduleReminderEmailOffsetDays,
            command.CommunicationPreference,
            nowUtc
        );
        if (created.IsFailure)
        {
            return Result.Failure<GroupMemberResponse>(created.Errors);
        }

        if (command.Attributes is not null)
        {
            foreach (var (key, value) in command.Attributes)
            {
                created.Value.SetAttributeValue(key, value);
            }
        }

        _ = dbContext.GroupMembers.Add(created.Value);
        await GroupMemberMapper.RotateHistoryIfEnabledAsync(
            dbContext,
            groupType,
            created.Value,
            role,
            nowUtc,
            cancellationToken
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            GroupMemberMapper.ToResponse(created.Value, group.Name, role, capacityWarning)
        );
    }

    private static Result<GroupTypeRole> ResolveRole(GroupType groupType, Guid? requestedRoleId)
    {
        var effectiveRoleId = requestedRoleId.HasValue
            ? GroupTypeRoleId.From(requestedRoleId.Value)
            : groupType.DefaultGroupRoleId;

        if (!effectiveRoleId.HasValue)
        {
            return GroupsErrors.DefaultRoleRequired;
        }

        var role = groupType.Roles.FirstOrDefault(r => r.Id == effectiveRoleId.Value);
        return role is null
            ? (Result<GroupTypeRole>)GroupsErrors.RoleMismatch
            : Result.Success(role);
    }

    private static Result<string?> EvaluateLimits(
        Group group,
        GroupType groupType,
        GroupTypeRole role,
        List<GroupMember> existingMembers,
        AddGroupMemberCommand command
    )
    {
        var activeInRole = existingMembers.Count(m =>
            m.GroupRoleId == role.Id && m.Status == GroupMemberStatus.Active
        );
        var activeUnits = GroupMembershipPolicy.ComputeActiveOccupancyUnits(
            existingMembers,
            group.AllowGuests
        );

        return GroupMembershipPolicy.EvaluateCapacityAndRoleLimits(
            group,
            groupType,
            role,
            command.Status,
            activeInRole,
            activeUnits,
            command.GuestCount
        );
    }
}
