using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Achievements;
using Ruth.Modules.Engagement.Slices.Events;
using Ruth.Modules.Engagement.Slices.Journeys;
using Ruth.Modules.Engagement.Slices.Streaks;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Engagement.Slices;

internal static class W5S2JourneyStreakRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.TryAddScoped<AchievementProgressionCollaborator>();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonAttendedIntegrationEvent,
            PersonAttendedStreakAndAchievementConsumer,
            EngagementDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            EngagementRequestConnectedIntegrationEvent,
            EngagementConnectedAchievementConsumer,
            EngagementDbContext
        >();

        RegisterJourneyHandlers(services);
        RegisterStreakHandlers(services);
        RegisterAchievementHandlers(services);
    }

    private static void RegisterJourneyHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateJourneyProgramCommand,
            JourneyProgramResponse,
            CreateJourneyProgramCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateJourneyProgramCommand,
            JourneyProgramResponse,
            UpdateJourneyProgramCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetJourneyProgramsQuery,
            IReadOnlyList<JourneyProgramResponse>,
            GetJourneyProgramsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetJourneyProgramByIdOrSlugQuery,
            JourneyProgramResponse,
            GetJourneyProgramByIdOrSlugQueryHandler
        >();
        _ = services.AddCommandHandler<
            RecordPersonMilestoneCommand,
            PersonMilestoneResponse,
            RecordPersonMilestoneCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonJourneyProgressQuery,
            IReadOnlyList<PersonJourneySummaryResponse>,
            GetPersonJourneyProgressQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetJourneyCompletionsQuery,
            IReadOnlyList<JourneyCompletionResponse>,
            GetJourneyCompletionsQueryHandler
        >();
    }

    private static void RegisterStreakHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateStreakTypeCommand,
            StreakTypeResponse,
            CreateStreakTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateStreakTypeCommand,
            StreakTypeResponse,
            UpdateStreakTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddStreakExclusionCommand,
            StreakTypeResponse,
            AddStreakExclusionCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetStreakTypesQuery,
            IReadOnlyList<StreakTypeResponse>,
            GetStreakTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            EnrollInStreakCommand,
            StreakResponse,
            EnrollInStreakCommandHandler
        >();
        _ = services.AddCommandHandler<
            RecordStreakEngagementCommand,
            StreakResponse,
            RecordStreakEngagementCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetStreaksQuery,
            IReadOnlyList<StreakResponse>,
            GetStreaksQueryHandler
        >();
    }

    private static void RegisterAchievementHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateAchievementTypeCommand,
            AchievementTypeResponse,
            CreateAchievementTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateAchievementTypeCommand,
            AchievementTypeResponse,
            UpdateAchievementTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAchievementTypesQuery,
            IReadOnlyList<AchievementTypeResponse>,
            GetAchievementTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            RecordAchievementProgressCommand,
            AchievementAttemptResponse,
            RecordAchievementProgressCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAchievementAttemptsQuery,
            IReadOnlyList<AchievementAttemptResponse>,
            GetAchievementAttemptsQueryHandler
        >();
    }
}
