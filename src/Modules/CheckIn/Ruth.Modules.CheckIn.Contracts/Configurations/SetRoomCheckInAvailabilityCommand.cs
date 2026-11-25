using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type SetRoomCheckInAvailabilityCommand in the CheckIn module.</summary>
public sealed record SetRoomCheckInAvailabilityCommand(
    Guid ConfigurationId,
    Guid AreaId,
    Guid LocationId,
    bool IsOpenForCheckIn,
    int? SoftCapacity = null,
    int? FirmCapacity = null
) : ICommand<CheckInConfigurationDetailResponse>;
