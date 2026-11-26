using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class GetBookableServicesQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetBookableServicesQuery, IReadOnlyList<BookableServiceResponse>>
{
    public async Task<Result<IReadOnlyList<BookableServiceResponse>>> HandleAsync(
        GetBookableServicesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.BookableServices.AsNoTracking();
        if (query.ActiveOnly)
        {
            q = q.Where(static s => s.IsActive);
        }

        if (query.PublicOnly)
        {
            q = q.Where(static s => s.IsPublic);
        }

        if (query.BranchId is { } b && b != Guid.Empty)
        {
            q = q.Where(s => s.BranchId == null || s.BranchId == b);
        }

        var list = await q.OrderBy(static s => s.Name).ToListAsync(cancellationToken);
        return list.Select(BookingsMapper.MapService).ToArray();
    }
}
