namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInConfigurationDetailResponse in the CheckIn module.</summary>
public sealed record CheckInConfigurationDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    CheckInProfileKind ProfileKind,
    Guid? BranchId,
    CheckInSearchMode SearchMode,
    int CheckInStartOffsetMinutes,
    int CheckInEndOffsetMinutes,
    int SecurityCodeAlphaLength,
    int SecurityCodeNumericLength,
    bool PreventDuplicateCheckIn,
    bool AllowCheckout,
    int AutoSelectDaysBack,
    bool IsActive,
    IReadOnlyList<CheckInAreaResponse> Areas,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
