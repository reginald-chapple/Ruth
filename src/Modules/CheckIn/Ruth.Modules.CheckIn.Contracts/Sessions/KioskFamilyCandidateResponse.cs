namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Matched household card containing evaluated member check-in options.</summary>
/// <param name="HouseholdId">Household identifier.</param>
/// <param name="HouseholdName">Household display name.</param>
/// <param name="MatchKind">How this household matched (Barcode, Phone, or Name).</param>
/// <param name="Members">Evaluated household members.</param>
public sealed record KioskFamilyCandidateResponse(
    Guid HouseholdId,
    string HouseholdName,
    string MatchKind,
    IReadOnlyList<KioskMemberCandidateResponse> Members
);
