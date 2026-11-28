using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Finance.Slices.Gateways;

internal static class FinancialGatewayMapper
{
    public static FinancialGatewayResponse ToResponse(
        FinancialGateway gateway,
        IPaymentGatewayRegistry registry
    )
    {
        return new FinancialGatewayResponse(
            gateway.Id.Value,
            gateway.Name,
            gateway.ProviderKey,
            registry.TryGet(gateway.ProviderKey, out _),
            gateway.Description,
            gateway.SupportsCards,
            gateway.SupportsAch,
            gateway.SupportsRecurring,
            gateway.IsTestMode,
            gateway.IsActive,
            gateway.BatchTimeOffsetHours,
            gateway.PublicTokenizerKey,
            SensitiveSettingRedactor.RedactDictionary(gateway.Settings),
            gateway.CreatedOnUtc
        );
    }
}
