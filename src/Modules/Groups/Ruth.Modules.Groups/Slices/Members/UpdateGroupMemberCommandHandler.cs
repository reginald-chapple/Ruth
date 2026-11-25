using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.Modules.Groups.Slices.Requirements;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class UpdateGroupMemberCommandHandler(
    GroupsDbContext dbContext,
    GroupRequirementEvaluatorService requirementEvaluator,
    IClock clock
) : ICommandHandler<UpdateGroupMemberCommand, GroupMemberResponse>
{
    public async Task<Result<GroupMemberResponse>> HandleAsync(
        UpdateGroupMemberCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var loaded = await LoadContextAsync(command, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<GroupMemberResponse>(loaded.Errors);
        }

        var (member, group, groupType, role) = loaded.Value;
        var eligibility = await ValidateUpdateEligibilityAsync(
            member,
            group,
            groupType,
            role,
            command,
            cancellationToken
        );
        return eligibility.IsFailure
            ? Result.Failure<GroupMemberResponse>(eligibility.Errors)
            : await ApplyAndSaveAsync(
                member,
                group,
                groupType,
                role,
                command,
                eligibility.Value,
                cancellationToken
            );
    }

    private async Task<Result<(GroupMember, Group, GroupType, GroupTypeRole)>> LoadContextAsync(
        UpdateGroupMemberCommand command,
        CancellationToken cancellationToken
    )
    {
        var memberId = GroupMemberId.From(command.GroupMemberId);
        var member = await dbContext.GroupMembers.FirstOrDefaultAsync(
            m => m.Id == memberId,
            cancellationToken
        );
        if (member is null)
        {
            return GroupsErrors.MemberNotFound;
        }

        var group = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == member.GroupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.Roles)
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var targetRoleId = GroupTypeRoleId.From(command.GroupRoleId);
        var role = groupType.Roles.FirstOrDefault(r => r.Id == targetRoleId);
        return role is null
            ? (Result<(GroupMember, Group, GroupType, GroupTypeRole)>)GroupsErrors.RoleMismatch
            : Result.Success((member, group, groupType, role));
    }

    private async Task<Result<string?>> ValidateUpdateEligibilityAsync(
        GroupMember member,
        Group group,
        GroupType groupType,
        GroupTypeRole role,
        UpdateGroupMemberCommand command,
        CancellationToken cancellationToken
    )
    {
        var otherMembers = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m => m.GroupId == member.GroupId && !m.IsArchived && m.Id != member.Id)
            .ToListAsync(cancellationToken);

        if (otherMembers.Exists(m => m.PersonId == member.PersonId && m.GroupRoleId == role.Id))
        {
            return GroupsErrors.DuplicateMemberRole;
        }

        var capacityCheck = GroupMembershipPolicy.EvaluateCapacityAndRoleLimits(
            group,
            groupType,
            role,
            command.Status,
            otherMembers.Count(m =>
                m.GroupRoleId == role.Id && m.Status == GroupMemberStatus.Active
            ),
            GroupMembershipPolicy.ComputeActiveOccupancyUnits(otherMembers, group.AllowGuests),
            command.GuestCount
        );
        if (capacityCheck.IsFailure)
        {
            return capacityCheck;
        }

        var needsReqCheck =
            !command.SkipRequirementsCheck
            && command.Status == GroupMemberStatus.Active
            && (member.Status != GroupMemberStatus.Active || member.GroupRoleId != role.Id);
        if (!needsReqCheck)
        {
            return capacityCheck;
        }

        var meets = await requirementEvaluator.CheckMustMeetRequirementsForAddAsync(
            group,
            member.PersonId,
            role.Id,
            cancellationToken
        );
        return meets ? capacityCheck : (Result<string?>)GroupsErrors.RequirementsNotMetForAdd;
    }

    private async Task<Result<GroupMemberResponse>> ApplyAndSaveAsync(
        GroupMember member,
        Group group,
        GroupType groupType,
        GroupTypeRole role,
        UpdateGroupMemberCommand command,
        string? capacityWarning,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var templateId = command.ScheduleTemplateId.HasValue
            ? GroupMemberScheduleTemplateId.From(command.ScheduleTemplateId.Value)
            : (GroupMemberScheduleTemplateId?)null;

        var updated = member.Update(
            group.AllowGuests,
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
        if (updated.IsFailure)
        {
            return Result.Failure<GroupMemberResponse>(updated.Errors);
        }

        await GroupMemberMapper.RotateHistoryIfEnabledAsync(
            dbContext,
            groupType,
            member,
            role,
            nowUtc,
            cancellationToken
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            GroupMemberMapper.ToResponse(member, group.Name, role, capacityWarning)
        );
    }
}
