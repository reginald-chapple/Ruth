using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class GetGroupDemographicTypesQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupDemographicTypesQuery, IReadOnlyList<GroupDemographicTypeResponse>>
{
    public async Task<Result<IReadOnlyList<GroupDemographicTypeResponse>>> HandleAsync(
        GetGroupDemographicTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var itemsQuery = dbContext.GroupDemographicTypes.AsNoTracking().AsQueryable();
        if (query.GroupTypeId.HasValue)
        {
            var typeId = GroupTypeId.From(query.GroupTypeId.Value);
            itemsQuery = itemsQuery.Where(t => t.GroupTypeId == typeId);
        }

        var items = await itemsQuery.OrderBy(static t => t.Name).ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupDemographicTypeResponse>>([
            .. items.Select(GroupMapper.ToDemographicTypeResponse),
        ]);
    }
}
