using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Contracts.Webhooks;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.Modules.VirtualRooms.Slices.Events;
using Ruth.Modules.VirtualRooms.Slices.Rooms;
using Ruth.Modules.VirtualRooms.Slices.Webhooks;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.VirtualRooms.Slices;

internal static class W5S7VirtualRoomsRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IVirtualRoomProvider, InMemoryVirtualRoomProvider>()
        );
        services.TryAddSingleton<VirtualRoomProviderSelector>();
        services.TryAddScoped<VirtualRoomAttendanceCollaborator>();
        services.TryAddScoped<LiveKitWebhookEgressCollaborator>();

        RegisterRoomAndSessionHandlers(services);
        RegisterRecordingWebhookAndConsumers(services);
    }

    private static void RegisterRoomAndSessionHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateVirtualRoomCommand,
            VirtualRoomResponse,
            CreateVirtualRoomCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateVirtualRoomCommand,
            VirtualRoomResponse,
            UpdateVirtualRoomCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetVirtualRoomsQuery,
            IReadOnlyList<VirtualRoomResponse>,
            GetVirtualRoomsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetVirtualRoomByIdOrSlugQuery,
            VirtualRoomResponse,
            GetVirtualRoomByIdOrSlugQueryHandler
        >();

        _ = services.AddCommandHandler<
            CreateVirtualRoomSessionCommand,
            VirtualRoomSessionResponse,
            CreateVirtualRoomSessionCommandHandler
        >();
        _ = services.AddCommandHandler<
            CloseVirtualRoomSessionCommand,
            VirtualRoomSessionResponse,
            CloseVirtualRoomSessionCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetVirtualRoomSessionsQuery,
            IReadOnlyList<VirtualRoomSessionResponse>,
            GetVirtualRoomSessionsQueryHandler
        >();
        _ = services.AddCommandHandler<
            MintVirtualRoomJoinTokenCommand,
            VirtualRoomJoinSessionResponse,
            MintVirtualRoomJoinTokenCommandHandler
        >();
    }

    private static void RegisterRecordingWebhookAndConsumers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            StartVirtualRoomRecordingCommand,
            VirtualRoomRecordingArchiveResponse,
            StartVirtualRoomRecordingCommandHandler
        >();
        _ = services.AddCommandHandler<
            StopVirtualRoomRecordingCommand,
            VirtualRoomRecordingArchiveResponse,
            StopVirtualRoomRecordingCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetVirtualRoomRecordingsQuery,
            IReadOnlyList<VirtualRoomRecordingArchiveResponse>,
            GetVirtualRoomRecordingsQueryHandler
        >();
        _ = services.AddCommandHandler<
            LiveKitWebhookIngestCommand,
            LiveKitWebhookIngestResponse,
            LiveKitWebhookIngestCommandHandler
        >();

        _ = services.AddIdempotentIntegrationEventConsumer<
            ServiceBookingConfirmedIntegrationEvent,
            ServiceBookingConfirmedVirtualRoomConsumer,
            VirtualRoomsDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            VirtualRoomsPersonMergedConsumer,
            VirtualRoomsDbContext
        >();
    }
}
