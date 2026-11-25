using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal static class CheckInConfigurationSliceRegistration
{
    public static void RegisterHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse,
            CreateCheckInConfigurationCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse,
            UpdateCheckInConfigurationCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpsertCheckInAreaCommand,
            CheckInConfigurationDetailResponse,
            UpsertCheckInAreaCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveCheckInAreaCommand,
            CheckInConfigurationDetailResponse,
            RemoveCheckInAreaCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetRoomCheckInAvailabilityCommand,
            CheckInConfigurationDetailResponse,
            SetRoomCheckInAvailabilityCommandHandler
        >();

        _ = services.AddQueryHandler<
            GetCheckInConfigurationsQuery,
            IReadOnlyList<CheckInConfigurationSummaryResponse>,
            GetCheckInConfigurationsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCheckInConfigurationByIdQuery,
            CheckInConfigurationDetailResponse,
            GetCheckInConfigurationByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            EvaluateCheckInOptionsQuery,
            CheckInEvaluationResponse,
            EvaluateCheckInOptionsQueryHandler
        >();
    }
}
