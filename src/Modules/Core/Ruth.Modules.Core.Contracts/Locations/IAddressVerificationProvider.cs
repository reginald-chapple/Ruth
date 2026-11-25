namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Modular provider contract for postal address standardization and geocoding.</summary>
public interface IAddressVerificationProvider
{
    /// <summary>Gets the provider display or service identifier.</summary>
    string ServiceName { get; }

    /// <summary>Gets the execution priority (lower runs first).</summary>
    int Order { get; }

    /// <summary>Gets whether this provider can standardize street addresses.</summary>
    bool SupportsStandardization { get; }

    /// <summary>Gets whether this provider can geocode addresses to WGS84 coordinates.</summary>
    bool SupportsGeocoding { get; }

    /// <summary>Attempts to standardize and/or geocode the supplied postal address.</summary>
    Task<AddressVerificationOutcome> VerifyAsync(
        LocationAddressDto address,
        CancellationToken cancellationToken = default
    );
}
