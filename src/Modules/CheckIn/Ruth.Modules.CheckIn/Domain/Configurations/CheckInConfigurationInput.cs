using Ruth.Modules.CheckIn.Contracts.Configurations;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

internal sealed record CheckInConfigurationInput(
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
    bool IsActive
);
