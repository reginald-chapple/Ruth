using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal sealed class GetGroupTypesQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupTypesQuery, IReadOnlyList<GroupTypeSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<GroupTypeSummaryResponse>>> HandleAsync(
        GetGroupTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var itemsQuery = dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.Roles)
            .AsQueryable();
        if (query.NavigationOnly)
        {
            itemsQuery = itemsQuery.Where(static gt => gt.ShowInNavigation);
        }

        var items = await itemsQuery
            .OrderBy(static gt => gt.Order)
            .ThenBy(static gt => gt.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupTypeSummaryResponse>>([
            .. items.Select(GroupTypeMapper.ToSummary),
        ]);
    }
}
