namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Postal street address fields for a location.</summary>
public sealed record LocationAddressDto(
    string? Street1 = null,
    string? Street2 = null,
    string? City = null,
    string? County = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null
);
