using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;

namespace Ruth.Modules.CheckIn.Domain.Sessions;

internal sealed record CheckInSessionCreationSpec(
    CheckInConfigurationId ConfigurationId,
    string ConfigurationName,
    Guid? BranchId,
    Guid? KioskDeviceId,
    Guid? HouseholdId,
    Guid? CheckedInByPersonId,
    CheckInSessionChannel Channel,
    CheckInSearchMode? SearchModeUsed,
    string? SearchTermSummary,
    DateOnly ServiceDate,
    string? SecurityPickupCode,
    string? MobileExpressToken,
    string? ClientIpAddress,
    DateTimeOffset CompletedOnUtc
);
