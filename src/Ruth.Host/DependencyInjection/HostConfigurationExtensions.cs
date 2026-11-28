using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.SharedKernel.Security;

namespace Ruth.Host.DependencyInjection;

internal static class HostConfigurationExtensions
{
    public static IServiceCollection AddRuthJwtConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .PostConfigure<IConfiguration>(
                static (options, configuration) =>
                    options.SigningKey = JwtOptions.ResolveSigningKey(configuration)
            )
            .ValidateOnStart();

        _ = services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        _ = services
            .AddOptions<TenantEnvelopeEncryptionOptions>()
            .BindConfiguration(TenantEnvelopeEncryptionOptions.SectionName)
            .PostConfigure<IConfiguration>(
                static (options, configuration) =>
                {
                    var explicitKek = configuration[
                        $"{TenantEnvelopeEncryptionOptions.SectionName}:MasterKey"
                    ];
                    if (string.IsNullOrWhiteSpace(explicitKek))
                    {
                        var jwtKey = JwtOptions.ResolveSigningKey(configuration);
                        if (!string.IsNullOrWhiteSpace(jwtKey))
                        {
                            options.MasterKey = $"ruth-derived-kek:{jwtKey.Trim()}";
                        }
                    }

                    TenantCredentialEncryptionContext.ConfigureDefault(
                        new AesGcmTenantCredentialProtector(options)
                    );
                }
            )
            .ValidateOnStart();

        _ = services.AddSingleton<ITenantCredentialProtector>(
            static sp => new AesGcmTenantCredentialProtector(
                sp.GetRequiredService<IOptions<TenantEnvelopeEncryptionOptions>>().Value
            )
        );

        return services;
    }
}
