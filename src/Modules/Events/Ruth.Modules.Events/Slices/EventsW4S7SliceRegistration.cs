using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.Modules.Events.Slices.Calendars;
using Ruth.Modules.Events.Slices.Events;
using Ruth.Modules.Events.Slices.InteractiveSessions;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Events.Slices;

internal static class EventsW4S7SliceRegistration
{
    public static void RegisterW4S7Services(IServiceCollection services)
    {
        _ = services.AddSignalR();
        services.TryAddSingleton<
            IInteractiveSessionBroadcaster,
            SignalRInteractiveSessionBroadcaster
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            EventsPersonMergedConsumer,
            EventsDbContext
        >();

        RegisterCalendarHandlers(services);
        RegisterInteractiveHandlers(services);
    }

    private static void RegisterCalendarHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateEventCalendarCommand,
            EventCalendarResponse,
            CreateEventCalendarCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateEventCalendarCommand,
            EventCalendarResponse,
            UpdateEventCalendarCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetEventCalendarsQuery,
            IReadOnlyList<EventCalendarResponse>,
            GetEventCalendarsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateEventItemCommand,
            EventItemResponse,
            CreateEventItemCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateEventItemCommand,
            EventItemResponse,
            UpdateEventItemCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddEventItemOccurrenceCommand,
            EventItemOccurrenceResponse,
            AddEventItemOccurrenceCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateEventItemOccurrenceCommand,
            EventItemOccurrenceResponse,
            UpdateEventItemOccurrenceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetEventItemByIdQuery,
            EventItemResponse,
            GetEventItemByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetEventItemsQuery,
            IReadOnlyList<EventItemResponse>,
            GetEventItemsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCalendarIcsFeedQuery,
            CalendarFeedPayloadResponse,
            GetCalendarIcsFeedQueryHandler
        >();
    }

    private static void RegisterInteractiveHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateInteractiveSessionCommand,
            InteractiveSessionDetailResponse,
            CreateInteractiveSessionCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddInteractiveActionCommand,
            InteractiveActionResponse,
            AddInteractiveActionCommandHandler
        >();
        _ = services.AddCommandHandler<
            StartInteractiveSessionOccurrenceCommand,
            InteractiveSessionDetailResponse,
            StartInteractiveSessionOccurrenceCommandHandler
        >();
        _ = services.AddCommandHandler<
            EndInteractiveSessionCommand,
            InteractiveSessionDetailResponse,
            EndInteractiveSessionCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetActiveInteractiveActionCommand,
            InteractiveStageProjectionResponse,
            SetActiveInteractiveActionCommandHandler
        >();
        _ = services.AddCommandHandler<
            SubmitInteractiveResponseCommand,
            InteractiveStageProjectionResponse,
            SubmitInteractiveResponseCommandHandler
        >();
        _ = services.AddCommandHandler<
            ModerateInteractiveResponseCommand,
            InteractiveStageProjectionResponse,
            ModerateInteractiveResponseCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetInteractiveSessionsQuery,
            IReadOnlyList<InteractiveSessionDetailResponse>,
            GetInteractiveSessionsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetInteractiveSessionStageQuery,
            InteractiveStageProjectionResponse,
            GetInteractiveSessionStageQueryHandler
        >();
    }
}
