using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Slices.Branches;
using Ruth.Modules.Core.Slices.Devices;
using Ruth.Modules.Core.Slices.Locations;
using Ruth.Modules.Core.Slices.Schedules;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Core;

internal static class CoreLocationsAndSchedulesRegistration
{
    public static void RegisterBranchAndLocationHandlers(IServiceCollection services)
    {
        RegisterBranchHandlers(services);
        RegisterLocationHandlers(services);
        RegisterDeviceHandlers(services);
        RegisterScheduleHandlers(services);
    }

    private static void RegisterBranchHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetBranchesQuery,
            IReadOnlyList<BranchResponse>,
            GetBranchesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetBranchByIdQuery,
            BranchResponse,
            GetBranchByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateBranchCommand,
            BranchResponse,
            CreateBranchCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateBranchCommand,
            BranchResponse,
            UpdateBranchCommandHandler
        >();
        _ = services.AddCommandHandler<DeleteBranchCommand, bool, DeleteBranchCommandHandler>();
    }

    private static void RegisterLocationHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetLocationTreeQuery,
            IReadOnlyList<LocationTreeNodeResponse>,
            GetLocationTreeQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetLocationByIdQuery,
            LocationResponse,
            GetLocationByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            FindLocationsWithinRadiusQuery,
            IReadOnlyList<LocationDistanceResponse>,
            FindLocationsWithinRadiusQueryHandler
        >();
        _ = services.AddQueryHandler<
            FindLocationsContainingPointQuery,
            IReadOnlyList<LocationResponse>,
            FindLocationsContainingPointQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateLocationCommand,
            LocationResponse,
            CreateLocationCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateLocationCommand,
            LocationResponse,
            UpdateLocationCommandHandler
        >();
        _ = services.AddCommandHandler<
            MoveLocationCommand,
            LocationResponse,
            MoveLocationCommandHandler
        >();
        _ = services.AddCommandHandler<
            VerifyLocationAddressCommand,
            LocationResponse,
            VerifyLocationAddressCommandHandler
        >();
        _ = services.AddCommandHandler<DeleteLocationCommand, bool, DeleteLocationCommandHandler>();
    }

    private static void RegisterDeviceHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetDevicesQuery,
            IReadOnlyList<DeviceResponse>,
            GetDevicesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetDeviceByIdQuery,
            DeviceResponse,
            GetDeviceByIdQueryHandler
        >();
        _ = services.AddQueryHandler<MatchDeviceQuery, DeviceResponse, MatchDeviceQueryHandler>();
        _ = services.AddCommandHandler<
            CreateDeviceCommand,
            DeviceResponse,
            CreateDeviceCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateDeviceCommand,
            DeviceResponse,
            UpdateDeviceCommandHandler
        >();
        _ = services.AddCommandHandler<DeleteDeviceCommand, bool, DeleteDeviceCommandHandler>();
    }

    private static void RegisterScheduleHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetSchedulesQuery,
            IReadOnlyList<ScheduleResponse>,
            GetSchedulesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetScheduleByIdQuery,
            ScheduleResponse,
            GetScheduleByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetScheduleOccurrencesQuery,
            IReadOnlyList<ScheduleOccurrenceDto>,
            GetScheduleOccurrencesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateScheduleCommand,
            ScheduleResponse,
            CreateScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateScheduleCommand,
            ScheduleResponse,
            UpdateScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<DeleteScheduleCommand, bool, DeleteScheduleCommandHandler>();
        _ = services.AddQueryHandler<
            GetScheduleCategoryExclusionsQuery,
            IReadOnlyList<ScheduleCategoryExclusionResponse>,
            GetScheduleCategoryExclusionsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateScheduleCategoryExclusionCommand,
            ScheduleCategoryExclusionResponse,
            CreateScheduleCategoryExclusionCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteScheduleCategoryExclusionCommand,
            bool,
            DeleteScheduleCategoryExclusionCommandHandler
        >();
    }
}
