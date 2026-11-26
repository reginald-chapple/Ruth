using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class RecurringGatewaySyncHelper(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry gatewayRegistry
)
{
    public async Task<
        Result<(FinancialGateway Gateway, IPaymentGateway Provider, SavedPaymentMethod Method)>
    > LoadActiveGatewayAndMethodAsync(
        FinancialGatewayId gatewayId,
        SavedPaymentMethodId methodId,
        CancellationToken cancellationToken
    )
    {
        var gateway = await dbContext
            .FinancialGateways.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gatewayId, cancellationToken);
        if (gateway is null || !gateway.IsActive || !gateway.SupportsRecurring)
        {
            return FinanceErrors.GatewayInactive;
        }

        if (!gatewayRegistry.TryGet(gateway.ProviderKey, out var provider))
        {
            return FinanceErrors.GatewayProviderUnavailable;
        }

        var savedMethod = await dbContext
            .SavedPaymentMethods.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == methodId && m.IsActive && m.FinancialGatewayId == gatewayId,
                cancellationToken
            );
        return savedMethod is null
            ? FinanceErrors.PaymentMethodNotFound
            : (gateway, provider, savedMethod);
    }

    public static async Task<Result<string>> SyncSubscriptionAsync(
        IPaymentGateway provider,
        bool isTestMode,
        string vaultToken,
        RecurringScheduleTransaction schedule,
        GatewaySubscriptionAction action,
        CancellationToken cancellationToken
    )
    {
        var synced = await provider.SyncSubscriptionAsync(
            new GatewaySubscriptionSyncRequest(
                schedule.Id.Value,
                vaultToken,
                schedule.TotalAmount,
                schedule.CurrencyCode,
                schedule.Frequency,
                schedule.NextPaymentDate,
                action,
                schedule.GatewaySubscriptionReference,
                isTestMode
            ),
            cancellationToken
        );
        return synced.IsFailure
            ? Result.Failure<string>(synced.Errors)
            : synced.Value.GatewaySubscriptionReference;
    }
}
