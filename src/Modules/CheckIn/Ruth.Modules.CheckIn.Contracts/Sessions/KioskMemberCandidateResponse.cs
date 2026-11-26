using Ruth.Modules.CheckIn.Contracts.Configurations;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Evaluated check-in candidate within a matched household.</summary>
/// <param name="PersonId">Person identifier.</param>
/// <param name="FullName">Display name.</param>
/// <param name="NickName">Preferred nickname.</param>
/// <param name="LastName">Last name.</param>
/// <param name="AgeYears">Age in years when known.</param>
/// <param name="GradeFormatted">Formatted grade label when applicable.</param>
/// <param name="AlreadyCheckedIn">Whether the person already has an active check-in for this schedule and date.</param>
/// <param name="PreselectedAreaId">Auto-selected area identifier from recent attendance within AutoSelectDaysBack.</param>
/// <param name="PreselectedLocationId">Balanced recommended location identifier for the preselected area.</param>
/// <param name="MedicalOrAllergyNote">Medical or allergy alert text when present.</param>
/// <param name="ActiveSignalNotes">Active compliance/safety signal notes.</param>
/// <param name="AreaOptions">All evaluated area and room options for this participant.</param>
public sealed record KioskMemberCandidateResponse(
    Guid PersonId,
    string FullName,
    string NickName,
    string LastName,
    decimal? AgeYears,
    string? GradeFormatted,
    bool AlreadyCheckedIn,
    Guid? PreselectedAreaId,
    Guid? PreselectedLocationId,
    string? MedicalOrAllergyNote,
    IReadOnlyList<string> ActiveSignalNotes,
    IReadOnlyList<EvaluatedCheckInAreaOptionResponse> AreaOptions
);
