using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class SetGroupArchiveStateCommandHandler(GroupsDbContext dbContext, IClock clock)
    : ICommandHandler<SetGroupArchiveStateCommand, GroupDetailResponse>
{
    public async Task<Result<GroupDetailResponse>> HandleAsync(
        SetGroupArchiveStateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var group = await dbContext
            .Groups.Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var nowUtc = clock.UtcNow;
        var result = group.SetArchiveState(command.IsArchived, command.ArchivedByPersonId, nowUtc);
        if (result.IsFailure)
        {
            return Result.Failure<GroupDetailResponse>(result.Errors);
        }

        await GroupHistoryHelper.RotateSnapshotIfEnabledAsync(
            dbContext,
            group,
            groupType,
            nowUtc,
            cancellationToken
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await GroupMapper.LoadDetailResponseAsync(
                dbContext,
                group,
                groupType.Name,
                cancellationToken
            )
        );
    }
}
