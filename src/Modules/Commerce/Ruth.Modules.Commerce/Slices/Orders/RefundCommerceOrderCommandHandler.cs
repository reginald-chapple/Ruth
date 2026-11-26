using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class RefundCommerceOrderCommandHandler(
    CommerceDbContext dbContext,
    CommerceOrderFinanceCollaborator financeCollaborator,
    IClock clock
) : ICommandHandler<RefundCommerceOrderCommand, CommerceOrderResponse>
{
    public async Task<Result<CommerceOrderResponse>> HandleAsync(
        RefundCommerceOrderCommand command,
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

        var recorded = order.RecordRefund(command.Amount);
        if (recorded.IsFailure)
        {
            return Result.Failure<CommerceOrderResponse>(recorded.Errors);
        }

        var gatewayRes = await financeCollaborator.RefundOrderTransactionAsync(
            order,
            command.Amount,
            command.Reason,
            command.ProcessedByPersonId,
            cancellationToken
        );
        if (gatewayRes.IsFailure)
        {
            return Result.Failure<CommerceOrderResponse>(gatewayRes.Errors);
        }

        var nowUtc = clock.UtcNow;
        if (order.Status == CommerceOrderStatus.Refunded)
        {
            await ReverseOrderEntitlementsAndStockAsync(
                order,
                command.RestockInventory,
                command.Reason,
                nowUtc,
                cancellationToken
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CommerceOrderMapper.MapOrder(order);
    }

    private async Task ReverseOrderEntitlementsAndStockAsync(
        CommerceOrder order,
        bool restockInventory,
        string reason,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (restockInventory)
        {
            var variantIds = order.Lines.Select(static l => l.VariantId).Distinct().ToArray();
            var variants = await dbContext
                .ProductVariants.Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(static v => v.Id, cancellationToken);
            foreach (var line in order.Lines)
            {
                if (variants.TryGetValue(line.VariantId, out var variant))
                {
                    variant.Restock(line.Quantity);
                }
            }
        }

        var passIds = order
            .Lines.Where(static l => l.IssuedPunchPassId.HasValue)
            .Select(static l => l.IssuedPunchPassId!.Value)
            .ToArray();
        if (passIds.Length == 0)
        {
            return;
        }

        var passes = await dbContext
            .IssuedPunchPasses.Include(static p => p.LedgerEntries)
            .Where(p => passIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var pass in passes)
        {
            var reversed = pass.RevokeForRefund($"Refund {order.OrderNumber}: {reason}", nowUtc);
            if (reversed > 0)
            {
                dbContext.EnqueueIntegrationEvent(
                    new PunchPassBalanceChangedIntegrationEvent(
                        Guid.CreateVersion7(),
                        nowUtc,
                        pass.Id.Value,
                        pass.PassCode,
                        pass.OwnerPersonId,
                        -reversed,
                        0,
                        0
                    )
                );
            }
        }
    }
}
