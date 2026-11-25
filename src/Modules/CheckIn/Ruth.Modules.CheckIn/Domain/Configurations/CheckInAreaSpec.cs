using Ruth.Modules.CheckIn.Contracts.Configurations;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

internal sealed record CheckInAreaSpec(
    string Name,
    string? Description,
    Guid? LinkedGroupId,
    RoomBalancingStrategy BalancingStrategy,
    int Order,
    bool IsActive,
    bool PrintBadge,
    bool IssueSecurityPickupCode,
    CheckInEligibilityRuleDto? Eligibility,
    IReadOnlyList<CheckInRoomMatrixCellInputDto>? Rooms
);
