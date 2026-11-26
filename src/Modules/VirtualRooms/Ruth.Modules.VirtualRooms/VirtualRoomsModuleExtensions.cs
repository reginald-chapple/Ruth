using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.Modules.VirtualRooms.Slices;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.VirtualRooms;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the VirtualRooms module.</summary>
public static class VirtualRoomsModuleExtensions
{
    /// <summary>Registers the VirtualRooms module DbContext, bootstrap migrations, entity catalog, providers, and W5-S7 handlers.</summary>
    public static IServiceCollection AddVirtualRoomsModule(
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

        _ = services.AddModuleDbContext<VirtualRoomsDbContext>(
            connectionString,
            VirtualRoomsDbContext.SchemaName
        );
        _ = services.AddHostedService<VirtualRoomsBootstrapHostedService>();

        W5S7VirtualRoomsRegistration.Register(services);

        foreach (var entityType in VirtualRoomsEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the VirtualRooms module.</summary>
    public static IEndpointRouteBuilder MapVirtualRoomsModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapVirtualRoomsEndpoints();
        return endpoints;
    }
}
