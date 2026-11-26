using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Providers.PaymentGateways.Nmi;
using Ruth.Providers.PaymentGateways.Stripe;

namespace Ruth.Providers.PaymentGateways;

/// <summary>Registers modular NMI and Stripe payment gateway providers.</summary>
public static class PaymentGatewayProvidersExtensions
{
    /// <summary>Adds NMI Customer Vault/Direct Post and Stripe v1 payment gateway implementations to DI.</summary>
    public static IServiceCollection AddPaymentGatewayProviders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.Configure<NmiGatewayOptions>(
            configuration.GetSection(NmiGatewayOptions.SectionName)
        );
        _ = services.Configure<StripeGatewayOptions>(
            configuration.GetSection(StripeGatewayOptions.SectionName)
        );

        _ = services.AddHttpClient(NmiGatewayProvider.HttpClientName);
        _ = services.AddHttpClient(StripePaymentGatewayProvider.HttpClientName);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPaymentGateway, NmiGatewayProvider>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPaymentGateway, StripePaymentGatewayProvider>()
        );

        return services;
    }
}
