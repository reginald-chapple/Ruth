namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInConfigurationSummaryResponse in the CheckIn module.</summary>
public sealed record CheckInConfigurationSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    CheckInProfileKind ProfileKind,
    Guid? BranchId,
    CheckInSearchMode SearchMode,
    bool IsActive,
    int AreaCount,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
