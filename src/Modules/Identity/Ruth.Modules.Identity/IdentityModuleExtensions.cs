using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Onboarding;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Slices.Authentication;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.Modules.Identity.Slices.Security;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Metadata;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity;

/// <summary>Service registration and endpoint mapping extensions for the Identity module.</summary>
public static class IdentityModuleExtensions
{
    private static readonly EntityTypeRegistration[] _entityTypes =
    [
        new(
            GlobalDefaultSecured.SystemGuid,
            GlobalDefaultSecured.TypeKey,
            nameof(GlobalDefaultSecured),
            "Global Default Security",
            "Identity",
            IsEntity: false,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            new Guid(0x0fa592f1, 0x728c, 0x4885, 0xbe, 0x38, 0x60, 0xed, 0x6c, 0x0d, 0x83, 0x4f),
            "Ruth.Identity.UserAccount",
            nameof(UserAccount),
            "User Account",
            "Identity",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            new Guid(0x9b5a6f82, 0x2413, 0x4b12, 0x9c, 0x01, 0x4d, 0x72, 0xe8, 0x1a, 0x55, 0x10),
            "Ruth.Identity.SecurityRole",
            nameof(SecurityRole),
            "Security Role",
            "Identity",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            new Guid(0x84855f4e, 0x9865, 0x4f1b, 0xb4, 0x20, 0x93, 0x93, 0x18, 0x27, 0x20, 0x04),
            "Ruth.Identity.EntityAclRule",
            nameof(EntityAclRule),
            "Entity ACL Rule",
            "Identity",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
        new(
            new Guid(0xcbd66c3a, 0x959a, 0x4a0b, 0x92, 0x6c, 0xc3, 0xad, 0xe4, 0x30, 0x66, 0xb1),
            "Ruth.Identity.AuthClient",
            nameof(AuthClient),
            "OAuth/OIDC Client",
            "Identity",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            new Guid(0x8926fe3b, 0x5ab6, 0x4e48, 0x81, 0x91, 0x09, 0xeb, 0x16, 0x82, 0xf7, 0x43),
            "Ruth.Identity.AuthScope",
            nameof(AuthScope),
            "OAuth/OIDC Scope",
            "Identity",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
    ];

    /// <summary>Registers the Identity module persistence context, authentication/ACL services, and slice handlers.</summary>
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
        services.TryAddSingleton<ITokenService, JwtTokenService>();
        services.TryAddSingleton<IAclSnapshotCache, AclSnapshotCache>();
        services.TryAddSingleton<IEntityAuthorizationService, EntityAuthorizationService>();
        _ = services.AddHttpClient(HttpExternalOidcClient.HttpClientName);
        services.TryAddScoped<IExternalOidcClient, HttpExternalOidcClient>();

        _ = services.AddIdentityAuthenticationAndAuthorization(configuration);
        _ = services.AddHostedService<IdentityBootstrapHostedService>();

        RegisterOnboardingHandlers(services);
        _ = services.AddAuthenticationHandlers();
        _ = services.AddSecurityHandlers();

        foreach (var entityType in _entityTypes)
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps HTTP endpoints owned by the Identity module.</summary>
    public static IEndpointRouteBuilder MapIdentityModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapOnboardingEndpoints();
        _ = endpoints.MapAuthEndpoints();
        _ = endpoints.MapAccountManagementEndpoints();
        _ = endpoints.MapOidcAuthEndpoints();
        _ = endpoints.MapSecurityEndpoints();
        _ = endpoints.MapAclManagementEndpoints();
        _ = endpoints.MapOAuthRegistryEndpoints();
        return endpoints;
    }

    private static void RegisterOnboardingHandlers(IServiceCollection services)
    {
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
    }
}
