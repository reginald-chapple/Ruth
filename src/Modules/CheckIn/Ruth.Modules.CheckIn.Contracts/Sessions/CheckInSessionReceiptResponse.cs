namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Completed check-in session receipt including issued daily security pickup code and express token.</summary>
/// <param name="SessionId">Created check-in session identifier.</param>
/// <param name="ConfigurationId">Check-in configuration identifier.</param>
/// <param name="ConfigurationName">Check-in configuration name.</param>
/// <param name="Channel">Session channel.</param>
/// <param name="ServiceDate">Local service date.</param>
/// <param name="HouseholdId">Optional household identifier.</param>
/// <param name="SecurityPickupCode">Collision-free daily security pickup code when issued.</param>
/// <param name="MobileExpressToken">Short scan token for mobile pre-check-in express kiosk printing.</param>
/// <param name="CompletedOnUtc">UTC completion timestamp.</param>
/// <param name="Entries">Checked-in attendance entries created in this session.</param>
public sealed record CheckInSessionReceiptResponse(
    Guid SessionId,
    Guid ConfigurationId,
    string ConfigurationName,
    CheckInSessionChannel Channel,
    DateOnly ServiceDate,
    Guid? HouseholdId,
    string? SecurityPickupCode,
    string? MobileExpressToken,
    DateTimeOffset CompletedOnUtc,
    IReadOnlyList<CheckedInEntryReceiptDto> Entries
);
