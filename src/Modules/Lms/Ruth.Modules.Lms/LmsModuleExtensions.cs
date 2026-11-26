using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.Modules.Lms.Slices;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Lms;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the LMS module.</summary>
public static class LmsModuleExtensions
{
    public static IServiceCollection AddLmsModule(
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

        _ = services.AddModuleDbContext<LmsDbContext>(connectionString, LmsDbContext.SchemaName);
        _ = services.AddHostedService<LmsBootstrapHostedService>();

        LmsSlicesRegistration.Register(services);

        foreach (var entityType in LmsEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapLmsModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapLmsEndpoints();
        return endpoints;
    }
}
