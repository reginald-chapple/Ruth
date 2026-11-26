namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Search results for a check-in kiosk or mobile pre-check-in flow.</summary>
/// <param name="ConfigurationId">Check-in configuration identifier.</param>
/// <param name="ConfigurationName">Check-in configuration display name.</param>
/// <param name="ScheduleId">Target schedule identifier.</param>
/// <param name="ServiceDate">Local service date evaluated.</param>
/// <param name="Families">Matched households and constituent candidates.</param>
public sealed record KioskSearchResponse(
    Guid ConfigurationId,
    string ConfigurationName,
    Guid ScheduleId,
    DateOnly ServiceDate,
    IReadOnlyList<KioskFamilyCandidateResponse> Families
);
