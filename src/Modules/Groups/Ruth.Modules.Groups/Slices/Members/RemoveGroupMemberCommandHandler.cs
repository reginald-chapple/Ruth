using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class RemoveGroupMemberCommandHandler(GroupsDbContext dbContext, IClock clock)
    : ICommandHandler<RemoveGroupMemberCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RemoveGroupMemberCommand command,
        CancellationToken cancellationToken = default
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

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.Roles)
            .FirstOrDefaultAsync(gt => gt.Id == member.GroupTypeId, cancellationToken);
        var role = groupType?.Roles.FirstOrDefault(r => r.Id == member.GroupRoleId);
        var nowUtc = clock.UtcNow;

        if (command.ArchiveInsteadOfDelete)
        {
            member.Archive(command.ArchivedByPersonId, nowUtc);
            if (groupType is not null && role is not null)
            {
                await GroupMemberMapper.RotateHistoryIfEnabledAsync(
                    dbContext,
                    groupType,
                    member,
                    role,
                    nowUtc,
                    cancellationToken
                );
            }
        }
        else
        {
            _ = dbContext.GroupMembers.Remove(member);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
