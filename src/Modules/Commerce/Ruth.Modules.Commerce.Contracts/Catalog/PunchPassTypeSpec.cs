namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Input specification for creating or updating a multi-visit or credit pack PunchPassType.</summary>
/// <param name="Name">Display name of the punch pass or credit pack type.</param>
/// <param name="Code">Unique normalized code (for example GYM-10-PACK).</param>
/// <param name="CreditUnits">Positive number of visits or credits granted upon issuance.</param>
/// <param name="ValidityDays">Optional validity window in days from issuance (null for non-expiring).</param>
/// <param name="AllowedBranchId">Optional Branch/Site restriction (null for organization-wide).</param>
/// <param name="AllowHouseholdSharing">When true, household members of the owner may redeem credits.</param>
/// <param name="Description">Optional summary description.</param>
/// <param name="IsActive">Whether new passes of this type may be sold or issued.</param>
public sealed record PunchPassTypeSpec(
    string Name,
    string Code,
    int CreditUnits,
    int? ValidityDays = null,
    Guid? AllowedBranchId = null,
    bool AllowHouseholdSharing = false,
    string? Description = null,
    bool IsActive = true
);
