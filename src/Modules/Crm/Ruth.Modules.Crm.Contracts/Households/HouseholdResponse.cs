namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Complete household (family) detail including members, locations, computed salutations, and giving leader.</summary>
public sealed record HouseholdResponse(
    Guid Id,
    string Name,
    Guid? CampusId,
    bool IsActive,
    bool IsArchived,
    bool CombineGivingByDefault,
    string Salutation,
    string SalutationFull,
    Guid? GivingLeaderPersonId,
    IReadOnlyList<HouseholdMemberResponse> Members,
    IReadOnlyList<HouseholdLocationResponse> Locations,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
