using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CreateCheckInConfigurationCommand in the CheckIn module.</summary>
public sealed record CreateCheckInConfigurationCommand(
    string Name,
    string? Description = null,
    CheckInProfileKind ProfileKind = CheckInProfileKind.MemberFacilityAccess,
    Guid? BranchId = null,
    CheckInSearchMode SearchMode = CheckInSearchMode.PhoneAndName,
    int CheckInStartOffsetMinutes = 30,
    int CheckInEndOffsetMinutes = 30,
    int SecurityCodeAlphaLength = 2,
    int SecurityCodeNumericLength = 2,
    bool PreventDuplicateCheckIn = true,
    bool AllowCheckout = true,
    int AutoSelectDaysBack = 60,
    bool IsActive = true
) : ICommand<CheckInConfigurationDetailResponse>;
