using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Sync;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Sync;

internal sealed class UpsertGroupSyncRuleCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<UpsertGroupSyncRuleCommand, GroupSyncRuleResponse>
{
    public async Task<Result<GroupSyncRuleResponse>> HandleAsync(
        UpsertGroupSyncRuleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var roleId = GroupTypeRoleId.From(command.GroupTypeRoleId);

        var group = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var roleExists = await dbContext.GroupTypeRoles.AnyAsync(
            r => r.Id == roleId && r.GroupTypeId == group.GroupTypeId,
            cancellationToken
        );
        if (!roleExists)
        {
            return GroupsErrors.RoleMismatch;
        }

        var upsert = await UpsertRuleAsync(groupId, roleId, command, cancellationToken);
        if (upsert.IsFailure)
        {
            return Result.Failure<GroupSyncRuleResponse>(upsert.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(upsert.Value));
    }

    internal static GroupSyncRuleResponse ToResponse(GroupSyncRule rule)
    {
        return new GroupSyncRuleResponse(
            rule.Id.Value,
            rule.GroupId.Value,
            rule.GroupTypeRoleId.Value,
            rule.SourceType,
            rule.SourceReferenceId,
            rule.SourceFilterValue,
            rule.ScheduleIntervalMinutes,
            rule.AddMissingMembers,
            rule.RemoveExtraMembers,
            rule.TargetMemberStatus,
            rule.LastSyncOnUtc,
            rule.LastAddedCount,
            rule.LastRemovedCount
        );
    }

    private async Task<Result<GroupSyncRule>> UpsertRuleAsync(
        GroupId groupId,
        GroupTypeRoleId roleId,
        UpsertGroupSyncRuleCommand command,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext.GroupSyncRules.FirstOrDefaultAsync(
            s => s.GroupId == groupId && s.GroupTypeRoleId == roleId,
            cancellationToken
        );
        if (existing is not null)
        {
            var updated = existing.Update(
                command.SourceType,
                command.SourceReferenceId,
                command.SourceFilterValue,
                command.ScheduleIntervalMinutes,
                command.AddMissingMembers,
                command.RemoveExtraMembers,
                command.TargetMemberStatus
            );
            return updated.IsFailure
                ? Result.Failure<GroupSyncRule>(updated.Errors)
                : Result.Success(existing);
        }

        var created = GroupSyncRule.Create(
            groupId,
            roleId,
            command.SourceType,
            command.SourceReferenceId,
            command.SourceFilterValue,
            command.ScheduleIntervalMinutes,
            command.AddMissingMembers,
            command.RemoveExtraMembers,
            command.TargetMemberStatus
        );
        if (created.IsSuccess)
        {
            _ = dbContext.GroupSyncRules.Add(created.Value);
        }

        return created;
    }
}
