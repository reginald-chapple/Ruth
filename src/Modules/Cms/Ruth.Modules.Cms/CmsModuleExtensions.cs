using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Cms.Slices;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Cms;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Cms module.</summary>
public static class CmsModuleExtensions
{
    /// <summary>Registers the Cms module DbContext, migrations, entity catalog, providers, buffers, and W6-S1..S4 handlers.</summary>
    public static IServiceCollection AddCmsModule(
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

        _ = services.AddModuleDbContext<CmsDbContext>(connectionString, CmsDbContext.SchemaName);
        _ = services.AddHostedService<CmsBootstrapHostedService>();

        CmsSlicesRegistration.Register(services);

        foreach (var entityType in CmsEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the Cms module (W6-S1 through W6-S4).</summary>
    public static IEndpointRouteBuilder MapCmsModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapCmsEngineEndpoints();
        _ = endpoints.MapCmsContentEndpoints();
        _ = endpoints.MapCmsPersonalizationEndpoints();
        _ = endpoints.MapCmsChannelsEndpoints();
        return endpoints;
    }
}
