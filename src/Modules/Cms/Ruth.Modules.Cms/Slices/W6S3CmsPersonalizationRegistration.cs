using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.Modules.Cms.Slices.Events;
using Ruth.Modules.Cms.Slices.Personalization;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S3CmsPersonalizationRegistration
{
    public static void Register(IServiceCollection services)
    {
        RegisterServices(services);
        RegisterInteractionHandlers(services);
        RegisterSegmentAndAdaptiveHandlers(services);
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.TryAddScoped<InteractionBatchPersister>();
        services.TryAddScoped<AdaptiveMessageContextLoader>();
        services.TryAddSingleton<IInteractionBuffer, BufferedInteractionChannel>();
        _ = services.AddHostedService<InteractionFlushBackgroundService>();
        services.TryAddSingleton<IPersonalizationSegmentCache, RedisPersonalizationSegmentCache>();
        services.TryAddScoped<PersonalizationSegmentEvaluator>();

        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            CmsPersonMergedConsumer,
            CmsDbContext
        >();
    }

    private static void RegisterInteractionHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateInteractionChannelCommand,
            InteractionChannelResponse,
            CreateInteractionChannelCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetInteractionChannelsQuery,
            IReadOnlyList<InteractionChannelResponse>,
            GetInteractionChannelsQueryHandler
        >();
        _ = services.AddCommandHandler<
            IngestInteractionsCommand,
            InteractionBufferStatusResponse,
            IngestInteractionsCommandHandler
        >();
        _ = services.AddCommandHandler<
            FlushInteractionBufferCommand,
            InteractionBufferStatusResponse,
            FlushInteractionBufferCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetInteractionsQuery,
            IReadOnlyList<InteractionResponse>,
            GetInteractionsQueryHandler
        >();
    }

    private static void RegisterSegmentAndAdaptiveHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreatePersonalizationSegmentCommand,
            PersonalizationSegmentResponse,
            CreatePersonalizationSegmentCommandHandler
        >();
        _ = services.AddCommandHandler<
            EvaluatePersonalizationSegmentCommand,
            PersonalizationSegmentResponse,
            EvaluatePersonalizationSegmentCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonalizationSegmentsQuery,
            IReadOnlyList<PersonalizationSegmentResponse>,
            GetPersonalizationSegmentsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateAdaptiveMessageCommand,
            AdaptiveMessageResponse,
            CreateAdaptiveMessageCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddAdaptiveMessageAdaptationCommand,
            AdaptiveMessageResponse,
            AddAdaptiveMessageAdaptationCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAdaptiveMessagesQuery,
            IReadOnlyList<AdaptiveMessageResponse>,
            GetAdaptiveMessagesQueryHandler
        >();
        _ = services.AddQueryHandler<
            ResolveAdaptiveMessageQuery,
            ResolvedAdaptiveMessageResponse,
            ResolveAdaptiveMessageQueryHandler
        >();
    }
}
