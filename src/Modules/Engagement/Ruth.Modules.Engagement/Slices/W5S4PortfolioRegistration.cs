using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Events;
using Ruth.Modules.Engagement.Slices.Touchpoints;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Engagement.Slices;

internal static class W5S4PortfolioRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.TryAddScoped<RelationshipHealthCrmCollaborator>();
        _ = services.AddScheduledJob<TouchpointCadenceGenerationJob>();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            EngagementPersonMergedConsumer,
            EngagementDbContext
        >();

        _ = services.AddCommandHandler<
            UpsertConstituentContactCommand,
            ConstituentContactResponse,
            UpsertConstituentContactCommandHandler
        >();
        _ = services.AddCommandHandler<
            ScheduleTouchpointCommand,
            TouchpointResponse,
            ScheduleTouchpointCommandHandler
        >();
        _ = services.AddCommandHandler<
            CompleteTouchpointCommand,
            TouchpointResponse,
            CompleteTouchpointCommandHandler
        >();
        _ = services.AddCommandHandler<
            SnoozeTouchpointCommand,
            TouchpointResponse,
            SnoozeTouchpointCommandHandler
        >();
        _ = services.AddCommandHandler<
            SkipTouchpointCommand,
            TouchpointResponse,
            SkipTouchpointCommandHandler
        >();
        _ = services.AddCommandHandler<
            GeneratePortfolioCadenceTouchpointsCommand,
            TouchpointCadenceRunResponse,
            GeneratePortfolioCadenceTouchpointsCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetMyPortfolioOutreachQueueQuery,
            PortfolioOutreachQueueResponse,
            GetMyPortfolioOutreachQueueQueryHandler
        >();
    }
}
