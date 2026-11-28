using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Domain.PunchPasses;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.PunchPasses;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class SubmitCommerceOrderCommandHandler(
    CommerceDbContext dbContext,
    CommerceCartEvaluationCollaborator evaluator,
    CommerceOrderFinanceCollaborator financeCollaborator,
    PunchPassCreditCalculator creditCalculator,
    IClock clock
) : ICommandHandler<SubmitCommerceOrderCommand, CommerceOrderResponse>
{
    public async Task<Result<CommerceOrderResponse>> HandleAsync(
        SubmitCommerceOrderCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = clock.UtcNow;
        var planRes = await evaluator.EvaluateCartAsync(
            command.Items,
            command.BuyerPersonId,
            command.ExplicitMembershipTier,
            command.FulfillmentMethod,
            command.TaxRateOverridePercent,
            trackCatalogEntities: true,
            nowUtc,
            cancellationToken
        );
        if (planRes.IsFailure)
        {
            return Result.Failure<CommerceOrderResponse>(planRes.Errors);
        }

        var orderRes = await BuildAndSettleOrderAsync(
            command,
            planRes.Value,
            nowUtc,
            cancellationToken
        );
        if (orderRes.IsFailure)
        {
            return Result.Failure<CommerceOrderResponse>(orderRes.Errors);
        }

        var order = orderRes.Value;
        var issuedPasses = await IssuePunchPassesIfSettledAsync(
            order,
            !command.RequestInvoiceBilling,
            nowUtc,
            cancellationToken
        );
        if (issuedPasses.IsFailure)
        {
            return Result.Failure<CommerceOrderResponse>(issuedPasses.Errors);
        }

        await PersistAndSyncAsync(order, issuedPasses.Value, nowUtc, cancellationToken);
        return CommerceOrderMapper.MapOrder(order);
    }

    private async Task<Result<CommerceOrder>> BuildAndSettleOrderAsync(
        SubmitCommerceOrderCommand command,
        EvaluatedCommerceOrderPlan plan,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var buyerIdRes = await ResolveBuyerIfPunchPassesOrInvoiceNeededAsync(
            command,
            plan,
            cancellationToken
        );
        if (buyerIdRes.IsFailure)
        {
            return Result.Failure<CommerceOrder>(buyerIdRes.Errors);
        }

        var orderRes = CommerceOrder.Create(
            buyerIdRes.Value,
            command.BuyerName,
            command.BuyerEmail,
            command.BranchId,
            command.ShippingAddressSummary,
            plan,
            nowUtc
        );
        if (orderRes.IsFailure)
        {
            return orderRes;
        }

        var deductRes = DeductTrackedInventory(plan);
        if (deductRes.IsFailure)
        {
            return Result.Failure<CommerceOrder>(deductRes.Errors);
        }

        var settleRes = await financeCollaborator.SettleOrderAsync(
            orderRes.Value,
            plan,
            command,
            nowUtc,
            cancellationToken
        );
        return settleRes.IsFailure ? Result.Failure<CommerceOrder>(settleRes.Errors) : orderRes;
    }

    private async Task<Result<Guid?>> ResolveBuyerIfPunchPassesOrInvoiceNeededAsync(
        SubmitCommerceOrderCommand command,
        EvaluatedCommerceOrderPlan plan,
        CancellationToken cancellationToken
    )
    {
        var needsPerson =
            command.RequestInvoiceBilling
            || plan.Lines.Any(static l => l.Product.Kind == ProductKind.PunchPassPack);
        if (!needsPerson)
        {
            return Result.Success(command.BuyerPersonId);
        }

        var res = await financeCollaborator.EnsureConstituentPersonAsync(
            command.BuyerPersonId,
            command.BuyerName,
            command.BuyerEmail,
            command.BranchId,
            cancellationToken
        );
        return res.IsFailure ? Result.Failure<Guid?>(res.Errors) : res.Value;
    }

    private static Result DeductTrackedInventory(EvaluatedCommerceOrderPlan plan)
    {
        foreach (var line in plan.Lines)
        {
            var res = line.Variant.DeductStock(line.Quantity);
            if (res.IsFailure)
            {
                return res;
            }
        }

        return Result.Success();
    }

    private async Task<Result<List<IssuedPunchPass>>> IssuePunchPassesIfSettledAsync(
        CommerceOrder order,
        bool isSettledNow,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var issued = new List<IssuedPunchPass>();
        if (!isSettledNow || order.BuyerPersonId is not { } ownerPersonId)
        {
            return issued;
        }

        var passLines = order
            .Lines.Where(static l =>
                l.ProductKind == ProductKind.PunchPassPack && l.PunchPassTypeId.HasValue
            )
            .ToArray();
        if (passLines.Length == 0)
        {
            return issued;
        }

        var typeIds = passLines.Select(static l => l.PunchPassTypeId!.Value).Distinct().ToArray();
        var types = await dbContext
            .PunchPassTypes.Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        foreach (var line in passLines)
        {
            if (!types.TryGetValue(line.PunchPassTypeId!.Value, out var passType))
            {
                return CommerceErrors.PunchPassTypeNotFound;
            }

            var passRes = IssuedPunchPass.Issue(
                passType,
                ownerPersonId,
                line.Quantity,
                order.Id,
                $"Order {order.OrderNumber} ({line.Sku})",
                nowUtc
            );
            if (passRes.IsFailure)
            {
                return Result.Failure<List<IssuedPunchPass>>(passRes.Errors);
            }

            line.AttachIssuedPunchPass(passRes.Value.Id);
            issued.Add(passRes.Value);
        }

        return issued;
    }

    private async Task PersistAndSyncAsync(
        CommerceOrder order,
        List<IssuedPunchPass> issuedPasses,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        _ = dbContext.Orders.Add(order);
        foreach (var pass in issuedPasses)
        {
            _ = dbContext.IssuedPunchPasses.Add(pass);
        }

        dbContext.EnqueueIntegrationEvent(
            new CommerceOrderCompletedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                order.Id.Value,
                order.OrderNumber,
                order.BuyerPersonId,
                order.BuyerEmail,
                order.CurrencyCode,
                order.TotalAmount,
                order.NonDeductibleRevenueAmount,
                order.TaxDeductibleContributionAmount,
                order.LinkedFinancialTransactionId,
                order.LinkedInvoiceId
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        if (issuedPasses.Count > 0 && order.BuyerPersonId is { } buyerId)
        {
            await StagePunchPassEventsAndSyncWalletAsync(
                issuedPasses,
                buyerId,
                nowUtc,
                cancellationToken
            );
        }
    }

    private async Task StagePunchPassEventsAndSyncWalletAsync(
        List<IssuedPunchPass> issuedPasses,
        Guid buyerId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var totalCredits = await creditCalculator.ComputeActivePunchCreditsAsync(
            buyerId,
            nowUtc,
            cancellationToken
        );
        foreach (var pass in issuedPasses)
        {
            dbContext.EnqueueIntegrationEvent(
                new PunchPassBalanceChangedIntegrationEvent(
                    Guid.CreateVersion7(),
                    nowUtc,
                    pass.Id.Value,
                    pass.PassCode,
                    buyerId,
                    pass.InitialCredits,
                    pass.RemainingCredits,
                    totalCredits
                )
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
