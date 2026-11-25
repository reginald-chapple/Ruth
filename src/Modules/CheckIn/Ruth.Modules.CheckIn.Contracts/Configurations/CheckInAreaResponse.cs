namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInAreaResponse in the CheckIn module.</summary>
public sealed record CheckInAreaResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? LinkedGroupId,
    RoomBalancingStrategy BalancingStrategy,
    int Order,
    bool IsActive,
    bool PrintBadge,
    bool IssueSecurityPickupCode,
    CheckInEligibilityRuleDto Eligibility,
    IReadOnlyList<CheckInRoomMatrixCellResponse> Rooms
);
