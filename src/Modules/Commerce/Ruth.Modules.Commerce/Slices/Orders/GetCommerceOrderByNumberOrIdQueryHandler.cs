using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class GetCommerceOrderByNumberOrIdQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetCommerceOrderByNumberOrIdQuery, CommerceOrderResponse>
{
    public async Task<Result<CommerceOrderResponse>> HandleAsync(
        GetCommerceOrderByNumberOrIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var order = await GetOrderDigitalDownloadLinkQueryHandler.LoadOrderAsync(
            dbContext,
            query.OrderNumberOrId,
            cancellationToken
        );
        return order is null ? CommerceErrors.OrderNotFound : CommerceOrderMapper.MapOrder(order);
    }
}
