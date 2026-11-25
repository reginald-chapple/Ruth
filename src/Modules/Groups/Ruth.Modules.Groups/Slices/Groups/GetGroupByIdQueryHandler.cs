using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class GetGroupByIdQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupByIdQuery, GroupDetailResponse>
{
    public async Task<Result<GroupDetailResponse>> HandleAsync(
        GetGroupByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupId.From(query.GroupId);
        var group = await dbContext
            .Groups.AsNoTracking()
            .Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupTypeName = await dbContext
            .GroupTypes.AsNoTracking()
            .Where(gt => gt.Id == group.GroupTypeId)
            .Select(static gt => gt.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Group";

        return Result.Success(
            await GroupMapper.LoadDetailResponseAsync(
                dbContext,
                group,
                groupTypeName,
                cancellationToken
            )
        );
    }
}
