using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.Modules.Core.Infrastructure.Imaging;
using Ruth.Modules.Core.Infrastructure.Storage;
using Ruth.Modules.Core.Slices.AttributeMatrices;
using Ruth.Modules.Core.Slices.Attributes;
using Ruth.Modules.Core.Slices.Branches;
using Ruth.Modules.Core.Slices.Categories;
using Ruth.Modules.Core.Slices.DefinedTypes;
using Ruth.Modules.Core.Slices.Devices;
using Ruth.Modules.Core.Slices.EntityTypes;
using Ruth.Modules.Core.Slices.Files;
using Ruth.Modules.Core.Slices.Locations;
using Ruth.Modules.Core.Slices.Notes;
using Ruth.Modules.Core.Slices.Schedules;
using Ruth.Modules.Core.Slices.Tags;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Core;

/// <summary>Service registration and endpoint mapping extensions for the Core foundation module.</summary>
public static class CoreModuleExtensions
{
    /// <summary>Registers the Core module DbContext, PostGIS spatial support, caches, FieldTypes, storage/imaging, and slice handlers.</summary>
    public static IServiceCollection AddCoreModule(
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

        _ = services.AddModuleDbContext<CoreDbContext>(
            connectionString,
            CoreDbContext.SchemaName,
            static opts =>
            {
                opts.ConfigureDataSource = static ds => _ = ds.UseNetTopologySuite();
                opts.ConfigureNpgsql = static npgsql => _ = npgsql.UseNetTopologySuite();
            }
        );

        RegisterCoreInfrastructure(services);
        RegisterAllHandlers(services);

        foreach (var entityType in CoreEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps HTTP endpoints owned by the Core module.</summary>
    public static IEndpointRouteBuilder MapCoreModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapEntityTypeAndFieldTypeEndpoints();
        _ = endpoints.MapDefinedTypeEndpoints();
        _ = endpoints.MapCategoryEndpoints();
        _ = endpoints.MapAttributeEndpoints();
        _ = endpoints.MapAttributeMatrixEndpoints();
        _ = endpoints.MapBranchEndpoints();
        _ = endpoints.MapLocationEndpoints();
        _ = endpoints.MapDeviceEndpoints();
        _ = endpoints.MapScheduleEndpoints();
        _ = endpoints.MapBinaryFileTypeEndpoints();
        _ = endpoints.MapBinaryFileEndpoints();
        _ = endpoints.MapDocumentAndSignatureEndpoints();
        _ = endpoints.MapHistoryAndNotesEndpoints();
        _ = endpoints.MapTagsSetsPreferencesEndpoints();
        return endpoints;
    }

    private static void RegisterCoreInfrastructure(IServiceCollection services)
    {
        foreach (var plugin in BuiltInFieldTypes.CreateAll())
        {
            _ = services.AddSingleton(plugin);
        }

        services.TryAddSingleton<IFieldTypeRegistry, FieldTypeRegistry>();
        services.TryAddSingleton<ICoreReadCache, CoreReadCache>();
        services.TryAddSingleton<IImageProcessor, StbImageProcessor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStorageProvider, InMemoryStorageProvider>()
        );
        services.TryAddSingleton<IStorageProviderResolver, StorageProviderResolver>();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            CorePersonMergedConsumer,
            CoreDbContext
        >();
        services.TryAddScoped<CoreTenantSeedContributor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ITenantSeedContributor, CoreTenantSeedContributor>()
        );
        _ = services.AddHostedService<CoreBootstrapHostedService>();
    }

    private static void RegisterAllHandlers(IServiceCollection services)
    {
        CoreSliceRegistration.RegisterEntityAndDefinedTypeHandlers(services);
        CoreSliceRegistration.RegisterCategoryAndAttributeHandlers(services);
        CoreSliceRegistration.RegisterMatrixHandlers(services);
        CoreLocationsAndSchedulesRegistration.RegisterBranchAndLocationHandlers(services);
        CoreFilesAndCollaborationRegistration.RegisterFilesHandlers(services);
        CoreFilesAndCollaborationRegistration.RegisterCollaborationHandlers(services);
    }
}
