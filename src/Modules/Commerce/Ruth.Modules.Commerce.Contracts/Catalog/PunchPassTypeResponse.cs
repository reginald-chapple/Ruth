namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Read projection representing a configured punch pass or credit pack type.</summary>
/// <param name="Id">Unique punch pass type identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Code">Normalized uppercase code.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CreditUnits">Credits granted on issuance.</param>
/// <param name="ValidityDays">Optional validity window in days.</param>
/// <param name="AllowedBranchId">Optional Branch/Site restriction.</param>
/// <param name="AllowHouseholdSharing">Whether household sharing is permitted.</param>
/// <param name="IsActive">Whether the pass type is active.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record PunchPassTypeResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    int CreditUnits,
    int? ValidityDays,
    Guid? AllowedBranchId,
    bool AllowHouseholdSharing,
    bool IsActive,
    DateTimeOffset CreatedOnUtc
);
