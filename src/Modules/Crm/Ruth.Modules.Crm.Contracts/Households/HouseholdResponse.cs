namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Complete household detail including members, locations, computed salutations, and primary contact.</summary>
public sealed record HouseholdResponse(
    Guid Id,
    string Name,
    Guid? BranchId,
    bool IsActive,
    bool IsArchived,
    bool CombineContributionsByDefault,
    string Salutation,
    string SalutationFull,
    Guid? PrimaryContactPersonId,
    IReadOnlyList<HouseholdMemberResponse> Members,
    IReadOnlyList<HouseholdLocationResponse> Locations,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
