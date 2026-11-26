using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class GetCommerceOrdersQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetCommerceOrdersQuery, IReadOnlyList<CommerceOrderResponse>>
{
    public async Task<Result<IReadOnlyList<CommerceOrderResponse>>> HandleAsync(
        GetCommerceOrdersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<CommerceOrder> q = dbContext.Orders.AsNoTracking().Include(static o => o.Lines);
        if (query.BuyerPersonId is { } buyer && buyer != Guid.Empty)
        {
            q = q.Where(o => o.BuyerPersonId == buyer);
        }

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            q = q.Where(o => o.Status == status);
        }

        if (query.BranchId is { } b && b != Guid.Empty)
        {
            q = q.Where(o => o.BranchId == b);
        }

        var orders = await q.OrderByDescending(static o => o.CreatedOnUtc)
            .ToListAsync(cancellationToken);
        return orders.Select(CommerceOrderMapper.MapOrder).ToArray();
    }
}
