using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal static class CheckInSessionSliceRegistration
{
    public static void RegisterHandlers(IServiceCollection services)
    {
        _ = services.AddScoped<KioskSearchReadModelLoader>();
        _ = services.AddScoped<CheckInSessionCollaborator>();
        _ = services.AddScoped<CheckInExternalContextService>();
        _ = services.AddQueryHandler<
            KioskSearchCandidatesQuery,
            KioskSearchResponse,
            KioskSearchCandidatesQueryHandler
        >();
        _ = services.AddCommandHandler<
            SubmitCheckInSessionCommand,
            CheckInSessionReceiptResponse,
            SubmitCheckInSessionCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCheckInSessionByIdQuery,
            CheckInSessionReceiptResponse,
            GetCheckInSessionByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            LookupActiveCheckInsQuery,
            IReadOnlyList<CheckedInEntryReceiptDto>,
            LookupActiveCheckInsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CheckOutAttendanceCommand,
            CheckOutResultResponse,
            CheckOutAttendanceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetLiveRoomRosterQuery,
            LiveRoomRosterResponse,
            GetLiveRoomRosterQueryHandler
        >();
        _ = services.AddCommandHandler<
            OverrideCheckInRoomAssignmentCommand,
            CheckedInEntryReceiptDto,
            OverrideCheckInRoomAssignmentCommandHandler
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            CheckInPersonMergedConsumer,
            CheckInDbContext
        >();
    }
}
