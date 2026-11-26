using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.Modules.Finance.Slices.Gateways;
using Ruth.Modules.Finance.Slices.PaymentMethods;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Finance.Slices;

internal static class FinanceW4S2SliceRegistration
{
    public static void RegisterW4S2Services(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        _ = services.Configure<FinanceSecurityOptions>(
            configuration.GetSection(FinanceSecurityOptions.SectionName)
        );

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPaymentGateway, TestPaymentGatewayProvider>()
        );
        services.TryAddSingleton<IPaymentGatewayRegistry, PaymentGatewayRegistry>();

        RegisterGatewayHandlers(services);
        RegisterPaymentMethodHandlers(services);
    }

    private static void RegisterGatewayHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateFinancialGatewayCommand,
            FinancialGatewayResponse,
            CreateFinancialGatewayCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateFinancialGatewayCommand,
            FinancialGatewayResponse,
            UpdateFinancialGatewayCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialGatewaysQuery,
            IReadOnlyList<FinancialGatewayResponse>,
            GetFinancialGatewaysQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetRegisteredPaymentGatewayProvidersQuery,
            IReadOnlyList<RegisteredPaymentGatewayProviderResponse>,
            GetRegisteredPaymentGatewayProvidersQueryHandler
        >();
    }

    private static void RegisterPaymentMethodHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            VaultSavedPaymentMethodCommand,
            SavedPaymentMethodResponse,
            VaultSavedPaymentMethodCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetDefaultSavedPaymentMethodCommand,
            SavedPaymentMethodResponse,
            SetDefaultSavedPaymentMethodCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeactivateSavedPaymentMethodCommand,
            SavedPaymentMethodResponse,
            DeactivateSavedPaymentMethodCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSavedPaymentMethodsQuery,
            IReadOnlyList<SavedPaymentMethodResponse>,
            GetSavedPaymentMethodsQueryHandler
        >();
        _ = services.AddCommandHandler<
            RegisterBankAccountIdentifierCommand,
            BankAccountIdentifierResponse,
            RegisterBankAccountIdentifierCommandHandler
        >();
        _ = services.AddQueryHandler<
            ResolvePersonByBankAccountQuery,
            BankAccountIdentifierResponse,
            ResolvePersonByBankAccountQueryHandler
        >();
    }
}
