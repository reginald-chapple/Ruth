using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type UpsertCheckInAreaCommand in the CheckIn module.</summary>
public sealed record UpsertCheckInAreaCommand(
    Guid ConfigurationId,
    string Name,
    Guid? AreaId = null,
    string? Description = null,
    Guid? LinkedGroupId = null,
    RoomBalancingStrategy BalancingStrategy = RoomBalancingStrategy.LowestCapacityRatio,
    int Order = 0,
    bool IsActive = true,
    bool PrintBadge = true,
    bool IssueSecurityPickupCode = false,
    CheckInEligibilityRuleDto? Eligibility = null,
    IReadOnlyList<CheckInRoomMatrixCellInputDto>? Rooms = null
) : ICommand<CheckInConfigurationDetailResponse>;
