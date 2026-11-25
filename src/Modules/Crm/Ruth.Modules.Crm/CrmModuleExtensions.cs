using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.Modules.Crm.Slices.Assessments;
using Ruth.Modules.Crm.Slices.Duplicates;
using Ruth.Modules.Crm.Slices.Households;
using Ruth.Modules.Crm.Slices.People;
using Ruth.Modules.Crm.Slices.Relationships;
using Ruth.Modules.Crm.Slices.Signals;
using Ruth.Modules.Crm.Slices.Verification;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Crm;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the CRM module.</summary>
public static class CrmModuleExtensions
{
    /// <summary>Registers the CRM module DbContext, Apache AGE graph service, badge evaluators, duplicate scanner job, and slice handlers.</summary>
    public static IServiceCollection AddCrmModule(
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

        _ = services.AddModuleDbContext<CrmDbContext>(
            connectionString,
            CrmDbContext.SchemaName,
            static opts => opts.ConfigureDataSource = static _ => { }
        );

        RegisterInfrastructure(services);
        RegisterHandlers(services);

        foreach (var entityType in CrmEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the CRM module.</summary>
    public static IEndpointRouteBuilder MapCrmModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapPersonEndpoints();
        _ = endpoints.MapHouseholdEndpoints();
        _ = endpoints.MapRelationshipEndpoints();
        _ = endpoints.MapDuplicateAndMergeEndpoints();
        _ = endpoints.MapSignalAndBadgeEndpoints();
        _ = endpoints.MapAssessmentAndCheckEndpoints();
        _ = endpoints.MapVerificationAndViewedEndpoints();
        return endpoints;
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.TryAddSingleton<ICrmGraphService, ApacheAgeCrmGraphService>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IBackgroundCheckProvider, InMemoryBackgroundCheckProvider>()
        );
        services.TryAddSingleton<
            IBackgroundCheckProviderResolver,
            BackgroundCheckProviderResolver
        >();

        RegisterBuiltInBadgeEvaluators(services);
        _ = services.AddScheduledJob<PersonDuplicateScannerJob>();
        _ = services.AddHostedService<CrmBootstrapHostedService>();
    }

    private static void RegisterBuiltInBadgeEvaluators(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersonBadgeEvaluator, TopSignalBadgeEvaluator>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersonBadgeEvaluator, ConnectionStatusBadgeEvaluator>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersonBadgeEvaluator, AgeAndBirthdayBadgeEvaluator>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersonBadgeEvaluator, AssessmentSummaryBadgeEvaluator>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IPersonBadgeEvaluator,
                BackgroundCheckStatusBadgeEvaluator
            >()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersonBadgeEvaluator, ProtectionProfileBadgeEvaluator>()
        );
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        CrmCoreSliceRegistration.RegisterPeopleHandlers(services);
        CrmCoreSliceRegistration.RegisterHouseholdsAndRelationshipsHandlers(services);
        CrmCoreSliceRegistration.RegisterDuplicateAndMergeHandlers(services);
        CrmProtectionSliceRegistration.RegisterSignalsAndBadgesHandlers(services);
        CrmProtectionSliceRegistration.RegisterAssessmentsAndChecksHandlers(services);
        CrmProtectionSliceRegistration.RegisterVerificationAndViewedHandlers(services);
    }
}
