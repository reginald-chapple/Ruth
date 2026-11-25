namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Result payload returned by an <see cref="IAddressVerificationProvider" />.</summary>
public sealed record AddressVerificationOutcome(
    AddressVerificationStatus Status,
    string ServiceName,
    string? Message = null,
    LocationAddressDto? StandardizedAddress = null,
    GeoPointDto? GeoPoint = null,
    string? Barcode = null
);
