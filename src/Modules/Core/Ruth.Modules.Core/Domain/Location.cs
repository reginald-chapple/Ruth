using System.Globalization;
using NetTopologySuite.Geometries;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain;

/// <summary>
/// Hierarchical physical, named, or street-address location aggregate backed by PostGIS
/// geography(Point, 4326) and geography(Polygon, 4326).
/// </summary>
public sealed class Location
    : AggregateRoot<LocationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for Location.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Location";

    private Location() { }

    private Location(LocationId id, Guid systemGuid)
        : base(id)
    {
        SystemGuid = systemGuid;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the optional parent location identifier in the building/room tree.</summary>
    public LocationId? ParentLocationId { get; private set; }

    /// <summary>Gets the optional named-location display name.</summary>
    public string? Name { get; private set; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets whether this location is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether this is a named location (building, room, branch/site).</summary>
    public bool IsNamedLocation => !string.IsNullOrWhiteSpace(Name);

    /// <summary>Gets the optional location type defined value identifier.</summary>
    public Guid? LocationTypeValueId { get; private set; }

    /// <summary>Gets the optional image binary file identifier.</summary>
    public Guid? ImageId { get; private set; }

    /// <summary>Gets address line 1.</summary>
    public string? Street1 { get; private set; }

    /// <summary>Gets address line 2.</summary>
    public string? Street2 { get; private set; }

    /// <summary>Gets the city.</summary>
    public string? City { get; private set; }

    /// <summary>Gets the county.</summary>
    public string? County { get; private set; }

    /// <summary>Gets the state or province.</summary>
    public string? State { get; private set; }

    /// <summary>Gets the postal or ZIP code.</summary>
    public string? PostalCode { get; private set; }

    /// <summary>Gets the ISO country code or name.</summary>
    public string? Country { get; private set; }

    /// <summary>Gets the PostGIS WGS84 point coordinate.</summary>
    public Point? GeoPoint { get; private set; }

    /// <summary>Gets the PostGIS WGS84 geofence polygon.</summary>
    public Polygon? GeoFence { get; private set; }

    /// <summary>Gets whether automatic geocoding updates are locked.</summary>
    public bool IsGeoPointLocked { get; private set; }

    /// <summary>Gets the postal delivery point barcode.</summary>
    public string? Barcode { get; private set; }

    /// <summary>Gets the optional assessor parcel identifier.</summary>
    public string? AssessorParcelId { get; private set; }

    /// <summary>Gets the optional check-in label printer device identifier.</summary>
    public DeviceId? PrinterDeviceId { get; private set; }

    /// <summary>Gets the soft room capacity threshold (override allowed).</summary>
    public int? SoftRoomThreshold { get; private set; }

    /// <summary>Gets the hard room capacity threshold (no override).</summary>
    public int? FirmRoomThreshold { get; private set; }

    /// <summary>Gets the optional BLE proximity beacon identifier (0..65535).</summary>
    public int? BeaconId { get; private set; }

    /// <summary>Gets the timestamp of the last standardization attempt.</summary>
    public DateTimeOffset? StandardizeAttemptedOnUtc { get; private set; }

    /// <summary>Gets the service that last attempted standardization.</summary>
    public string? StandardizeAttemptedServiceType { get; private set; }

    /// <summary>Gets the result message from the last standardization attempt.</summary>
    public string? StandardizeAttemptedResult { get; private set; }

    /// <summary>Gets when the postal address was last successfully standardized.</summary>
    public DateTimeOffset? StandardizedOnUtc { get; private set; }

    /// <summary>Gets the timestamp of the last geocoding attempt.</summary>
    public DateTimeOffset? GeocodeAttemptedOnUtc { get; private set; }

    /// <summary>Gets the service that last attempted geocoding.</summary>
    public string? GeocodeAttemptedServiceType { get; private set; }

    /// <summary>Gets the result message from the last geocoding attempt.</summary>
    public string? GeocodeAttemptedResult { get; private set; }

    /// <summary>Gets when the location was last successfully geocoded.</summary>
    public DateTimeOffset? GeocodedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        IsNamedLocation && ParentLocationId.HasValue
            ? new SecuredEntityRef(
                EntityTypeKeyName,
                ParentLocationId.Value.Value,
                GlobalDefaultSecured.Instance
            )
            : GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Computes the formatted human-readable location display label.</summary>
    public string GetDisplayName(bool preferName = true)
    {
        if (preferName && !string.IsNullOrWhiteSpace(Name))
        {
            return Name;
        }

        var street = FormatStreetAddress();
        return !string.IsNullOrWhiteSpace(street) ? street
            : !string.IsNullOrWhiteSpace(Name) ? Name
            : GeoPoint is not null
                ? string.Create(
                    CultureInfo.InvariantCulture,
                    $"Point ({GeoPoint.Y:F5}, {GeoPoint.X:F5})"
                )
            : GeoFence is not null
                ? string.Create(
                    CultureInfo.InvariantCulture,
                    $"Geofence ({GeoFence.NumPoints} points)"
                )
            : string.Empty;
    }

    internal static Result<Location> Create(LocationInput input, Guid? systemGuid = null)
    {
        var id = LocationId.Create();
        var location = new Location(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value
        );
        var apply = location.Apply(input, updateParent: true);
        return apply.IsFailure ? Result.Failure<Location>(apply.Errors) : Result.Success(location);
    }

    internal Result Update(LocationInput input)
    {
        return Apply(input, updateParent: false);
    }

    internal Result MoveToParent(LocationId? newParentId)
    {
        if (newParentId.HasValue && newParentId.Value == Id)
        {
            return CoreLocationErrors.LocationCycleDetected;
        }

        ParentLocationId = newParentId;
        return Result.Success();
    }

    internal void ApplyVerificationOutcome(
        AddressVerificationOutcome outcome,
        DateTimeOffset nowUtc,
        bool allowStandardization,
        bool allowGeocoding
    )
    {
        var isConnectionError = outcome.Status.HasFlag(AddressVerificationStatus.ConnectionError);
        if (allowStandardization && !isConnectionError)
        {
            StandardizeAttemptedOnUtc = nowUtc;
            StandardizeAttemptedServiceType = outcome.ServiceName;
            StandardizeAttemptedResult = Truncate(outcome.Message, 200);
            if (
                outcome.Status.HasFlag(AddressVerificationStatus.Standardized)
                && outcome.StandardizedAddress is not null
            )
            {
                ApplyAddress(outcome.StandardizedAddress);
                if (!string.IsNullOrWhiteSpace(outcome.Barcode))
                {
                    Barcode = Truncate(outcome.Barcode.Trim(), 40);
                }

                StandardizedOnUtc = nowUtc;
            }
        }

        if (allowGeocoding && !isConnectionError && !IsGeoPointLocked)
        {
            GeocodeAttemptedOnUtc = nowUtc;
            GeocodeAttemptedServiceType = outcome.ServiceName;
            GeocodeAttemptedResult = Truncate(outcome.Message, 200);
            if (
                outcome.Status.HasFlag(AddressVerificationStatus.Geocoded)
                && outcome.GeoPoint is not null
            )
            {
                var pointResult = GeoGeometryHelper.CreatePoint(outcome.GeoPoint);
                if (pointResult.IsSuccess && pointResult.Value is not null)
                {
                    GeoPoint = pointResult.Value;
                    GeocodedOnUtc = nowUtc;
                }
            }
        }
    }

    private Result Apply(LocationInput input, bool updateParent)
    {
        var pointResult = GeoGeometryHelper.CreatePoint(input.GeoPoint);
        var polygonResult = GeoGeometryHelper.CreatePolygon(input.GeoFenceRing);
        var textValidation = ValidateTextAndNumericBounds(input);
        var combined = Result.Combine(pointResult, polygonResult, textValidation);
        if (combined.IsFailure)
        {
            return combined;
        }

        if (!HasAnyIdentity(input, pointResult.Value, polygonResult.Value))
        {
            return CoreLocationErrors.LocationEmpty;
        }

        if (updateParent)
        {
            ParentLocationId = input.ParentLocationId;
        }

        ApplyValidatedFields(input, pointResult.Value, polygonResult.Value);
        return Result.Success();
    }

    private static Result ValidateTextAndNumericBounds(LocationInput input)
    {
        var nameResult = CoreTextPolicy.ValidateOptionalText(
            input.Name,
            CoreTextPolicy.NameMaxLength,
            allowNewLines: false
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            input.Description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, descResult);
        return combined.IsFailure ? combined
            : input.SoftRoomThreshold is < 0
            || input.FirmRoomThreshold is < 0
            || (
                input.SoftRoomThreshold.HasValue
                && input.FirmRoomThreshold.HasValue
                && input.SoftRoomThreshold.Value > input.FirmRoomThreshold.Value
            )
                ? (Result)CoreLocationErrors.LocationThresholdsInvalid
            : input.BeaconId is < 0 or > ushort.MaxValue ? CoreLocationErrors.BranchBeaconInvalid
            : Result.Success();
    }

    private void ApplyValidatedFields(LocationInput input, Point? geoPoint, Polygon? geoFence)
    {
        Name = Normalize(input.Name, 100);
        IsActive = input.IsActive;
        LocationTypeValueId = input.LocationTypeValueId;
        ImageId = input.ImageId;
        ApplyAddress(input.Address);
        GeoPoint = geoPoint;
        GeoFence = geoFence;
        IsGeoPointLocked = input.IsGeoPointLocked;
        Barcode = Normalize(input.Barcode, 40);
        AssessorParcelId = Normalize(input.AssessorParcelId, 50);
        PrinterDeviceId = input.PrinterDeviceId;
        SoftRoomThreshold = input.SoftRoomThreshold;
        FirmRoomThreshold = input.FirmRoomThreshold;
        BeaconId = input.BeaconId;
        Description =
            !string.IsNullOrWhiteSpace(input.Description) ? input.Description.Trim()
            : !IsNamedLocation ? GetDisplayName(preferName: false)
            : null;
    }

    private void ApplyAddress(LocationAddressDto? address)
    {
        Street1 = Normalize(address?.Street1, 100);
        Street2 = Normalize(address?.Street2, 100);
        City = Normalize(address?.City, 50);
        County = Normalize(address?.County, 50);
        State = Normalize(address?.State, 50);
        PostalCode = Normalize(address?.PostalCode, 50);
        Country = Normalize(address?.Country, 50);
    }

    private static bool HasAnyIdentity(LocationInput input, Point? point, Polygon? polygon)
    {
        var addr = input.Address;
        var hasAddress =
            addr is not null
            && (
                !string.IsNullOrWhiteSpace(addr.Street1)
                || !string.IsNullOrWhiteSpace(addr.Street2)
                || !string.IsNullOrWhiteSpace(addr.City)
            );
        return !string.IsNullOrWhiteSpace(input.Name)
            || hasAddress
            || point is not null
            || polygon is not null;
    }

    private string FormatStreetAddress()
    {
        string?[] parts = [Street1, Street2, City, State, PostalCode, Country];
        return string.Join(", ", parts.Where(static p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) ? null
            : value.Length <= maxLength ? value
            : value[..maxLength];
    }
}
