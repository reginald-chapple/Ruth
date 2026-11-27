using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.Modules.Tenancy.Infrastructure.Verification;
using Ruth.Modules.Tenancy.Slices.Compliance;
using Ruth.Modules.Tenancy.Slices.Endpoints;
using Ruth.Modules.Tenancy.Slices.Operators;
using Ruth.Modules.Tenancy.Slices.Signup;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy;

/// <summary>Service registration and endpoint mapping extensions for the Tenancy platform control-plane module.</summary>
public static class TenancyModuleExtensions
{
    /// <summary>Registers Tenancy DbContext (platform schema), host resolution, IRS EO BMF &amp; Stripe Connect providers, and handlers.</summary>
    public static IServiceCollection AddTenancyModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "RuthDatabase"
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not found."
            );

        _ = services.AddOptions<TenancyOptions>().Bind(configuration.GetSection("Tenancy"));

        _ = services.AddModuleDbContext<TenancyDbContext>(
            connectionString,
            TenancyDbContext.SchemaName,
            static opts => opts.IsTenantScopedSchema = false
        );

        services.TryAddSingleton<ActiveTenantRegistry>();
        services.TryAddSingleton<IActiveTenantRegistry>(static sp =>
            sp.GetRequiredService<ActiveTenantRegistry>()
        );
        services.TryAddSingleton<ITenantHostResolver, TenantHostResolver>();
        services.TryAddSingleton<
            IIrsCharityVerificationProvider,
            SimulatedIrsEoBmfVerificationProvider
        >();
        services.TryAddSingleton<IStripeConnectProvider, MockStripeConnectProvider>();
        services.TryAddSingleton<SimulatedDnsTxtVerificationProvider>();
        services.TryAddSingleton<IDnsTxtVerificationProvider>(static sp =>
            sp.GetRequiredService<SimulatedDnsTxtVerificationProvider>()
        );
        _ = services.AddHostedService<TenancyBootstrapHostedService>();

        RegisterHandlers(services);
        return services;
    }

    /// <summary>Maps HTTP endpoints owned by the Tenancy module.</summary>
    public static IEndpointRouteBuilder MapTenancyModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapTenancySignupEndpoints();
        _ = endpoints.MapTenantComplianceEndpoints();
        _ = endpoints.MapPlatformOperatorEndpoints();
        return endpoints;
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        _ = services.AddRequestValidator<
            SignupTaxExemptTenantCommand,
            SignupTaxExemptTenantCommandValidator
        >();
        _ = services.AddCommandHandler<
            SignupTaxExemptTenantCommand,
            SignupTaxExemptTenantResponse,
            SignupTaxExemptTenantCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCurrentTenantComplianceQuery,
            TenantComplianceStatusResponse,
            GetCurrentTenantComplianceQueryHandler
        >();
        _ = services.AddCommandHandler<
            StartStripeConnectOnboardingCommand,
            StripeConnectOnboardingResponse,
            StartStripeConnectOnboardingCommandHandler
        >();
        _ = services.AddCommandHandler<
            CompleteMockStripeConnectCommand,
            TenantComplianceStatusResponse,
            CompleteMockStripeConnectCommandHandler
        >();
        _ = services.AddCommandHandler<
            RegisterCustomDomainCommand,
            TenantDomainResponse,
            RegisterCustomDomainCommandHandler
        >();
        _ = services.AddCommandHandler<
            VerifyCustomDomainCommand,
            TenantDomainResponse,
            VerifyCustomDomainCommandHandler
        >();
        _ = services.AddCommandHandler<
            SubmitTenantForGoLiveCommand,
            TenantComplianceStatusResponse,
            SubmitTenantForGoLiveCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPendingGoLiveQueueQuery,
            IReadOnlyList<TenantSummaryResponse>,
            GetPendingGoLiveQueueQueryHandler
        >();
        _ = services.AddCommandHandler<
            ReviewTenantGoLiveCommand,
            TenantComplianceStatusResponse,
            ReviewTenantGoLiveCommandHandler
        >();
        _ = services.AddCommandHandler<
            AuthenticatePlatformOperatorCommand,
            PlatformOperatorAuthResponse,
            AuthenticatePlatformOperatorCommandHandler
        >();
    }
}
