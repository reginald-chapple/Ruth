using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>Immutable snapshot of person attributes used for fast pairwise duplicate scoring.</summary>
public sealed record DuplicateCandidateProfile(
    PersonId PersonId,
    string FirstName,
    string NickName,
    string LastName,
    string? Suffix,
    string? NormalizedEmail,
    Gender Gender,
    MaritalStatus MaritalStatus,
    DateOnly? BirthDate,
    Guid? PrimaryCampusId,
    Guid? PrimaryHouseholdId,
    HouseholdRole? PrimaryHouseholdRole,
    Guid? PrimaryHomeLocationId,
    IReadOnlyList<string> CellPhoneKeys,
    IReadOnlyList<string> HomePhoneKeys,
    string FirstNameMetaphone,
    string LastNameMetaphone,
    IReadOnlySet<string> ExpandedFirstNames
);
