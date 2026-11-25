namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Bitwise outcome status reported by an address verification or geocoding provider.</summary>
[Flags]
public enum AddressVerificationStatus
{
    /// <summary>No verification action succeeded.</summary>
    None = 0,

    /// <summary>Postal address fields were standardized.</summary>
    Standardized = 1,

    /// <summary>Geographic coordinates were resolved.</summary>
    Geocoded = 2,

    /// <summary>A transient connectivity error prevented attempting verification.</summary>
    ConnectionError = 4,
}
