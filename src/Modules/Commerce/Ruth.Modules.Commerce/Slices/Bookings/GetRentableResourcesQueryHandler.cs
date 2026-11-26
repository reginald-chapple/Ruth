using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class GetRentableResourcesQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetRentableResourcesQuery, IReadOnlyList<RentableResourceResponse>>
{
    public async Task<Result<IReadOnlyList<RentableResourceResponse>>> HandleAsync(
        GetRentableResourcesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.RentableResources.AsNoTracking();
        if (query.ActiveOnly)
        {
            q = q.Where(static r => r.IsActive);
        }

        if (query.PublicOnly)
        {
            q = q.Where(static r => r.IsPublic);
        }

        if (query.BranchId is { } b && b != Guid.Empty)
        {
            q = q.Where(r => r.BranchId == null || r.BranchId == b);
        }

        var list = await q.OrderBy(static r => r.Name).ToListAsync(cancellationToken);
        return list.Select(BookingsMapper.MapResource).ToArray();
    }
}
