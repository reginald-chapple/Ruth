using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Badges;

internal sealed class GetBadgeDefinitionsQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetBadgeDefinitionsQuery, IReadOnlyList<BadgeDefinitionResponse>>
{
    public async Task<Result<IReadOnlyList<BadgeDefinitionResponse>>> HandleAsync(
        GetBadgeDefinitionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext.BadgeDefinitions.AsNoTracking();
        if (query.ActiveOnly)
        {
            q = q.Where(static b => b.IsActive);
        }

        var list = await q.OrderBy(static b => b.Order)
            .ThenBy(static b => b.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<BadgeDefinitionResponse> result =
        [
            .. list.Select(CreateBadgeDefinitionCommandHandler.ToResponse),
        ];
        return Result.Success(result);
    }
}
