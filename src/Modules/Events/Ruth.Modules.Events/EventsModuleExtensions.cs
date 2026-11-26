using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.Modules.Events.Slices;
using Ruth.Modules.Events.Slices.Calendars;
using Ruth.Modules.Events.Slices.InteractiveSessions;
using Ruth.Modules.Events.Slices.Registrations;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Events;

/// <summary>Service registration and Minimal API / SignalR endpoint mapping extensions for the Events module.</summary>
public static class EventsModuleExtensions
{
    public static IServiceCollection AddEventsModule(
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

        _ = services.AddModuleDbContext<EventsDbContext>(
            connectionString,
            EventsDbContext.SchemaName
        );
        services.TryAddScoped<EventsTenantSeedContributor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ITenantSeedContributor, EventsTenantSeedContributor>()
        );
        _ = services.AddHostedService<EventsBootstrapHostedService>();
        EventsW4S7SliceRegistration.RegisterW4S7Services(services);
        EventsW4S8SliceRegistration.RegisterW4S8Services(services);

        foreach (var entityType in EventsEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEventsModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapEventCalendarEndpoints();
        _ = endpoints.MapInteractiveSessionEndpoints();
        _ = endpoints.MapRegistrationEndpoints();
        _ = endpoints.MapHub<InteractiveSessionHub>("/hubs/events/interactive").AllowAnonymous();
        return endpoints;
    }
}
