using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type RemoveCheckInAreaCommand in the CheckIn module.</summary>
public sealed record RemoveCheckInAreaCommand(Guid ConfigurationId, Guid AreaId)
    : ICommand<CheckInConfigurationDetailResponse>;
