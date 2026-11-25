using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Sync;

internal sealed class GetGroupSyncRulesQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupSyncRulesQuery, IReadOnlyList<GroupSyncRuleResponse>>
{
    public async Task<Result<IReadOnlyList<GroupSyncRuleResponse>>> HandleAsync(
        GetGroupSyncRulesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var rulesQuery = dbContext.GroupSyncRules.AsNoTracking().AsQueryable();
        if (query.GroupId.HasValue)
        {
            var groupId = GroupId.From(query.GroupId.Value);
            rulesQuery = rulesQuery.Where(r => r.GroupId == groupId);
        }

        var rules = await rulesQuery.ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<GroupSyncRuleResponse>>([
            .. rules.Select(UpsertGroupSyncRuleCommandHandler.ToResponse),
        ]);
    }
}
