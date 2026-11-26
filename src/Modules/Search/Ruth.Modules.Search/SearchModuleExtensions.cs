using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Search.Infrastructure;
using Ruth.Modules.Search.Slices;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Search;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Search module.</summary>
public static class SearchModuleExtensions
{
    public static IServiceCollection AddSearchModule(
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

        _ = services.AddModuleDbContext<SearchDbContext>(
            connectionString,
            SearchDbContext.SchemaName
        );
        _ = services.AddHostedService<SearchBootstrapHostedService>();

        SearchSlicesRegistration.Register(services);

        foreach (var entityType in SearchEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapSearchModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapSearchEndpoints();
        return endpoints;
    }
}
