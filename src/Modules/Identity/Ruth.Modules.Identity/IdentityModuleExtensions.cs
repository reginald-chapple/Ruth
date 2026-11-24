using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Onboarding;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Identity;

/// <summary>Service registration and endpoint mapping extensions for the Identity module.</summary>
public static class IdentityModuleExtensions
{
    /// <summary>Registers the Identity module persistence context, security services, and onboarding handlers.</summary>
    public static IServiceCollection AddIdentityModule(
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

        _ = services.AddModuleDbContext<IdentityDbContext>(
            connectionString,
            IdentityDbContext.SchemaName
        );

        services.TryAddSingleton<IPasswordHashingService, AspNetPasswordHashingService>();
        services.TryAddSingleton<IOnboardingStateReader, OnboardingStateReader>();
        _ = services.AddHostedService<IdentityBootstrapHostedService>();

        _ = services.AddRequestValidator<
            CompleteOnboardingCommand,
            CompleteOnboardingCommandValidator
        >();
        _ = services.AddCommandHandler<
            CompleteOnboardingCommand,
            CompleteOnboardingResponse,
            CompleteOnboardingCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetOnboardingStatusQuery,
            OnboardingStatusResponse,
            GetOnboardingStatusQueryHandler
        >();

        return services;
    }

    /// <summary>Maps HTTP endpoints owned by the Identity module.</summary>
    public static IEndpointRouteBuilder MapIdentityModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.MapOnboardingEndpoints();
    }
}
