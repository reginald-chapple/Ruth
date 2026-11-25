using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class GetLocationScheduleConfigsQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<
        GetLocationScheduleConfigsQuery,
        IReadOnlyList<GroupLocationScheduleConfigResponse>
    >
{
    public async Task<Result<IReadOnlyList<GroupLocationScheduleConfigResponse>>> HandleAsync(
        GetLocationScheduleConfigsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(query.GroupId);
        var locations = await dbContext
            .GroupLocations.AsNoTracking()
            .Where(gl => gl.GroupId == groupId)
            .ToDictionaryAsync(static gl => gl.Id, cancellationToken);
        if (locations.Count == 0)
        {
            return Result.Success<IReadOnlyList<GroupLocationScheduleConfigResponse>>([]);
        }

        GroupLocationId[] glIds = [.. locations.Keys];
        var configs = await dbContext
            .LocationScheduleConfigs.AsNoTracking()
            .Where(c => glIds.Contains(c.GroupLocationId))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupLocationScheduleConfigResponse>>([
            .. configs.Select(c =>
                UpsertLocationScheduleConfigCommandHandler.ToResponse(
                    c,
                    locations[c.GroupLocationId]
                )
            ),
        ]);
    }
}
