using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices;
using Ruth.Modules.Engagement.Slices.Journeys;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.Modules.Engagement.Slices.Reminders;
using Ruth.Modules.Engagement.Slices.Streaks;
using Ruth.Modules.Engagement.Slices.SupportRequests;
using Ruth.Modules.Engagement.Slices.Touchpoints;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Engagement;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Engagement module.</summary>
public static class EngagementModuleExtensions
{
    /// <summary>Registers the Engagement module DbContext, bootstrap migrations, entity catalog, scheduled jobs, and slice handlers.</summary>
    public static IServiceCollection AddEngagementModule(
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

        _ = services.AddModuleDbContext<EngagementDbContext>(
            connectionString,
            EngagementDbContext.SchemaName
        );
        _ = services.AddHostedService<EngagementBootstrapHostedService>();

        W5S1PipelineRegistration.Register(services);
        W5S2JourneyStreakRegistration.Register(services);
        W5S3SupportReminderRegistration.Register(services);
        W5S4PortfolioRegistration.Register(services);

        foreach (var entityType in EngagementEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the Engagement module.</summary>
    public static IEndpointRouteBuilder MapEngagementModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapEngagementPipelineEndpoints();
        _ = endpoints.MapEngagementJourneyEndpoints();
        _ = endpoints.MapEngagementStreaksAndAchievementsEndpoints();
        _ = endpoints.MapEngagementSupportEndpoints();
        _ = endpoints.MapEngagementRemindersAndFollowingEndpoints();
        _ = endpoints.MapEngagementPortfolioEndpoints();
        return endpoints;
    }
}
