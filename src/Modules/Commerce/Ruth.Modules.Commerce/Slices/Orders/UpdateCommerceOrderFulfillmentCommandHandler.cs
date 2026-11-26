using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class UpdateCommerceOrderFulfillmentCommandHandler(CommerceDbContext dbContext)
    : ICommandHandler<UpdateCommerceOrderFulfillmentCommand, CommerceOrderResponse>
{
    public async Task<Result<CommerceOrderResponse>> HandleAsync(
        UpdateCommerceOrderFulfillmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = CommerceOrderId.From(command.OrderId);
        var order = await dbContext
            .Orders.Include(static o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return CommerceErrors.OrderNotFound;
        }

        var res = order.AdvanceFulfillment(command.NewStatus, command.TrackingReference);
        if (res.IsFailure)
        {
            return Result.Failure<CommerceOrderResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CommerceOrderMapper.MapOrder(order);
    }
}
