using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Providers.WalletPasses.Apple;
using Ruth.Providers.WalletPasses.Google;

namespace Ruth.Providers.WalletPasses;

/// <summary>Registers Apple .pkpass PKCS#7 and Google Wallet RS256 JWT providers.</summary>
public static class WalletPassProvidersServiceCollectionExtensions
{
    /// <summary>Adds Apple Wallet (.pkpass) and Google Wallet Save JWT providers bound to configuration.</summary>
    public static IServiceCollection AddWalletPassProviders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.Configure<ApplePkpassOptions>(
            configuration.GetSection(ApplePkpassOptions.SectionName)
        );
        _ = services.Configure<GoogleWalletOptions>(
            configuration.GetSection(GoogleWalletOptions.SectionName)
        );

        _ = services.AddSingleton<IWalletPassProvider, ApplePkpassProvider>();
        _ = services.AddSingleton<IWalletPassProvider, GoogleWalletJwtProvider>();

        return services;
    }
}
