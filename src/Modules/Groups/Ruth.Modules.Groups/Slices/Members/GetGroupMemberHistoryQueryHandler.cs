using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class GetGroupMemberHistoryQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupMemberHistoryQuery, IReadOnlyList<GroupMemberHistoricalResponse>>
{
    public async Task<Result<IReadOnlyList<GroupMemberHistoricalResponse>>> HandleAsync(
        GetGroupMemberHistoryQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var memberId = GroupMemberId.From(query.GroupMemberId);
        var rows = await dbContext
            .GroupMemberHistoricals.AsNoTracking()
            .Where(h => h.GroupMemberId == memberId)
            .OrderByDescending(static h => h.EffectiveDateTimeUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupMemberHistoricalResponse>>([
            .. rows.Select(GroupMemberMapper.ToHistoricalResponse),
        ]);
    }
}
