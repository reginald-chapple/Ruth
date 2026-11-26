namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Household unit and member profiles returned by <see cref="LookupKioskHouseholdsQuery" />.</summary>
/// <param name="HouseholdId">Household identifier (or synthetic person-scoped identifier when ungrouped).</param>
/// <param name="HouseholdName">Display name of the household or member unit.</param>
/// <param name="BranchId">Optional primary branch identifier.</param>
/// <param name="MatchKind">How this household matched (Barcode, Phone, or Name).</param>
/// <param name="MatchedPersonIds">Identifiers of members that directly matched the search input.</param>
/// <param name="Members">All active non-deceased members of the household.</param>
public sealed record KioskHouseholdLookupResponse(
    Guid HouseholdId,
    string HouseholdName,
    Guid? BranchId,
    string MatchKind,
    IReadOnlyList<Guid> MatchedPersonIds,
    IReadOnlyList<KioskPersonLookupDto> Members
);
