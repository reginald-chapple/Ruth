using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type GetCheckInConfigurationsQuery in the CheckIn module.</summary>
public sealed record GetCheckInConfigurationsQuery(Guid? BranchId = null, bool ActiveOnly = false)
    : IQuery<IReadOnlyList<CheckInConfigurationSummaryResponse>>;
