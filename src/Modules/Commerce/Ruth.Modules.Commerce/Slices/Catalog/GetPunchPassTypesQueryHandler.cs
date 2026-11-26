using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class GetPunchPassTypesQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetPunchPassTypesQuery, IReadOnlyList<PunchPassTypeResponse>>
{
    public async Task<Result<IReadOnlyList<PunchPassTypeResponse>>> HandleAsync(
        GetPunchPassTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.PunchPassTypes.AsNoTracking();
        if (query.ActiveOnly)
        {
            q = q.Where(static p => p.IsActive);
        }

        var items = await q.OrderBy(static p => p.Name).ToListAsync(cancellationToken);
        return items.Select(CatalogMapper.MapPunchPassType).ToArray();
    }
}
