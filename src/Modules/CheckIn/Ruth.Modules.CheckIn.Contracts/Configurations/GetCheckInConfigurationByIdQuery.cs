using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type GetCheckInConfigurationByIdQuery in the CheckIn module.</summary>
public sealed record GetCheckInConfigurationByIdQuery(Guid ConfigurationId)
    : IQuery<CheckInConfigurationDetailResponse>;
