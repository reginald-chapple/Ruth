using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class GetGroupHistoryQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupHistoryQuery, IReadOnlyList<GroupHistoricalResponse>>
{
    public async Task<Result<IReadOnlyList<GroupHistoricalResponse>>> HandleAsync(
        GetGroupHistoryQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(query.GroupId);
        var rows = await dbContext
            .GroupHistoricals.AsNoTracking()
            .Where(h => h.GroupId == groupId)
            .OrderByDescending(static h => h.EffectiveDateTimeUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupHistoricalResponse>>([
            .. rows.Select(GroupMapper.ToHistoricalResponse),
        ]);
    }
}
