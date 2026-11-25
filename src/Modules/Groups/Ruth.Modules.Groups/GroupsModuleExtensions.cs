using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.Modules.Groups.Slices.Attendances;
using Ruth.Modules.Groups.Slices.Groups;
using Ruth.Modules.Groups.Slices.GroupTypes;
using Ruth.Modules.Groups.Slices.Members;
using Ruth.Modules.Groups.Slices.Requirements;
using Ruth.Modules.Groups.Slices.Scheduling;
using Ruth.Modules.Groups.Slices.Sync;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Groups;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Groups module.</summary>
public static class GroupsModuleExtensions
{
    /// <summary>Registers the Groups module DbContext (with PostGIS), GroupSync job, PersonMerged consumer, and all slice handlers.</summary>
    public static IServiceCollection AddGroupsModule(
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

        _ = services.AddModuleDbContext<GroupsDbContext>(
            connectionString,
            GroupsDbContext.SchemaName,
            static opts =>
            {
                opts.ConfigureDataSource = static ds => _ = ds.UseNetTopologySuite();
                opts.ConfigureNpgsql = static npgsql => _ = npgsql.UseNetTopologySuite();
            }
        );

        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            GroupsPersonMergedConsumer,
            GroupsDbContext
        >();
        _ = services.AddScheduledJob<GroupSyncReconciliationJob>();
        _ = services.AddHostedService<GroupsBootstrapHostedService>();

        GroupsCoreSliceRegistration.RegisterGroupTypeHandlers(services);
        GroupsCoreSliceRegistration.RegisterGroupHandlers(services);
        GroupsCoreSliceRegistration.RegisterMemberAndSyncHandlers(services);
        GroupsSchedulingSliceRegistration.RegisterSchedulingHandlers(services);
        GroupsSchedulingSliceRegistration.RegisterAttendanceHandlers(services);

        foreach (var entityType in GroupsEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the Groups module.</summary>
    public static IEndpointRouteBuilder MapGroupsModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapGroupTypeEndpoints();
        _ = endpoints.MapGroupEndpoints();
        _ = endpoints.MapGroupMemberEndpoints();
        _ = endpoints.MapGroupRequirementEndpoints();
        _ = endpoints.MapGroupSyncEndpoints();
        _ = endpoints.MapSchedulingEndpoints();
        _ = endpoints.MapAttendanceEndpoints();
        return endpoints;
    }
}
