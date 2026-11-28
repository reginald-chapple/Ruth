using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal sealed class VaultSavedPaymentMethodCommandHandler(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry gatewayRegistry,
    ITenantContext tenantContext
) : ICommandHandler<VaultSavedPaymentMethodCommand, SavedPaymentMethodResponse>
{
    public async Task<Result<SavedPaymentMethodResponse>> HandleAsync(
        VaultSavedPaymentMethodCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var gatewayId = FinancialGatewayId.From(command.FinancialGatewayId);
        var gateway = await dbContext
            .FinancialGateways.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gatewayId, cancellationToken);
        if (gateway is null)
        {
            return FinanceErrors.GatewayNotFound;
        }

        if (!gateway.IsActive)
        {
            return FinanceErrors.GatewayInactive;
        }

        if (!gateway.IsTestMode && !tenantContext.IsLive)
        {
            return FinanceErrors.LiveChargeRequiresApprovedTenant;
        }

        if (!gatewayRegistry.TryGet(gateway.ProviderKey, out var provider))
        {
            return FinanceErrors.GatewayProviderUnavailable;
        }

        var vaultResult = await provider.VaultPaymentMethodAsync(
            BuildVaultRequest(command, gateway.IsTestMode),
            cancellationToken
        );
        if (vaultResult.IsFailure)
        {
            return Result.Failure<SavedPaymentMethodResponse>(vaultResult.Errors);
        }

        var created = CreateMethodEntity(command, gatewayId, vaultResult.Value);
        if (created.IsFailure)
        {
            return Result.Failure<SavedPaymentMethodResponse>(created.Errors);
        }

        if (command.SetAsDefault)
        {
            await ClearPreviousDefaultsAsync(command.PersonId, cancellationToken);
        }

        _ = dbContext.SavedPaymentMethods.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return SavedPaymentMethodMapper.ToResponse(created.Value, gateway.Name);
    }

    private static GatewayVaultRequest BuildVaultRequest(
        VaultSavedPaymentMethodCommand command,
        bool isTestMode
    )
    {
        return new GatewayVaultRequest(
            command.PersonId,
            command.HostedPaymentToken,
            command.MethodKind,
            command.BillingName,
            command.BillingEmail,
            command.MaskedLast4Hint,
            command.CardBrandHint,
            command.ExpiryMonthHint,
            command.ExpiryYearHint,
            isTestMode
        );
    }

    private static Result<SavedPaymentMethod> CreateMethodEntity(
        VaultSavedPaymentMethodCommand command,
        FinancialGatewayId gatewayId,
        GatewayCustomerVaultResult vaulted
    )
    {
        return SavedPaymentMethod.Create(
            command.PersonId,
            gatewayId,
            vaulted.CustomerVaultToken,
            command.MethodKind,
            command.DisplayName,
            vaulted.CardBrand,
            vaulted.MaskedAccountNumber,
            vaulted.ExpiryMonth,
            vaulted.ExpiryYear,
            command.HouseholdId,
            command.SetAsDefault
        );
    }

    private async Task ClearPreviousDefaultsAsync(
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        var previousDefaults = await dbContext
            .SavedPaymentMethods.Where(m => m.PersonId == personId && m.IsDefault)
            .ToListAsync(cancellationToken);
        foreach (var prev in previousDefaults)
        {
            prev.SetDefault(isDefault: false);
        }
    }
}
