using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices;
using Ruth.Modules.Commerce.Slices.Bookings;
using Ruth.Modules.Commerce.Slices.Catalog;
using Ruth.Modules.Commerce.Slices.Orders;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Commerce;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Commerce module.</summary>
public static class CommerceModuleExtensions
{
    /// <summary>Registers the Commerce module DbContext, bootstrap migrations, entity catalog, scheduled jobs, and slice handlers.</summary>
    public static IServiceCollection AddCommerceModule(
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

        _ = services.AddModuleDbContext<CommerceDbContext>(
            connectionString,
            CommerceDbContext.SchemaName
        );
        _ = services.AddHostedService<CommerceBootstrapHostedService>();
        CommerceSliceRegistration.RegisterAllServices(services);

        foreach (var entityType in CommerceEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the Commerce module.</summary>
    public static IEndpointRouteBuilder MapCommerceModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapCommerceCatalogEndpoints();
        _ = endpoints.MapCommerceOrderEndpoints();
        _ = endpoints.MapCommerceBookingEndpoints();
        return endpoints;
    }
}
