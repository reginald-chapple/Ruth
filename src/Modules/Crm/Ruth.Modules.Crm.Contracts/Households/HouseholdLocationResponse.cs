namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Represents a physical location link on a household.</summary>
public sealed record HouseholdLocationResponse(
    Guid Id,
    Guid LocationId,
    HouseholdLocationType LocationType,
    bool IsMailingLocation,
    bool IsMappedLocation,
    string? AddressSummary
);
