using Ruth.Modules.Core.Contracts.Devices;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain;

/// <summary>Physical kiosk, label printer, check-in tablet, or print proxy device aggregate.</summary>
public sealed class Device
    : AggregateRoot<DeviceId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for Device.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Device";

    private readonly List<DeviceLocation> _locations = [];

    private Device() { }

    private Device(DeviceId id, Guid systemGuid)
        : base(id)
    {
        SystemGuid = systemGuid;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the unique device name (max 50 chars).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized name for uniqueness checks.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the device type defined value identifier.</summary>
    public Guid DeviceTypeValueId { get; private set; }

    /// <summary>Gets the optional IPv4/IPv6 address or hostname (max 45 chars).</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Gets the uppercase normalized IP/host for uniqueness checks per device type.</summary>
    public string? NormalizedIpAddress { get; private set; }

    /// <summary>Gets whether the device is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the optional primary physical/geofenced location identifier.</summary>
    public LocationId? LocationId { get; private set; }

    /// <summary>Gets whether labels are printed from client or server.</summary>
    public DevicePrintFrom PrintFrom { get; private set; }

    /// <summary>Gets the printer target override behavior.</summary>
    public DevicePrintToOverride PrintToOverride { get; private set; }

    /// <summary>Gets the optional label printer device identifier.</summary>
    public DeviceId? PrinterDeviceId { get; private set; }

    /// <summary>Gets the optional cloud/local print proxy device identifier.</summary>
    public DeviceId? ProxyDeviceId { get; private set; }

    /// <summary>Gets whether the kiosk device has a camera.</summary>
    public bool HasCamera { get; private set; }

    /// <summary>Gets the optional camera barcode configuration.</summary>
    public CameraBarcodeConfiguration? CameraBarcodeConfigurationType { get; private set; }

    /// <summary>Gets the optional check-in kiosk platform type.</summary>
    public KioskType? KioskType { get; private set; }

    /// <summary>Gets the check-in locations served by this device.</summary>
    public IReadOnlyCollection<DeviceLocation> Locations => _locations.AsReadOnly();

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
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    internal static Result<Device> Create(DeviceInput input, Guid? systemGuid = null)
    {
        var id = DeviceId.Create();
        var device = new Device(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value
        );
        var apply = device.Apply(input);
        return apply.IsFailure ? Result.Failure<Device>(apply.Errors) : Result.Success(device);
    }

    internal Result Update(DeviceInput input)
    {
        return Apply(input);
    }

    private Result Apply(DeviceInput input)
    {
        if (input.DeviceTypeValueId == Guid.Empty)
        {
            return CoreLocationErrors.DeviceTypeRequired;
        }

        var nameResult = CoreTextPolicy.ValidateRequiredText(
            input.Name,
            maxLength: 50,
            CoreLocationErrors.DeviceNameRequired,
            CoreLocationErrors.DeviceNameTooLong
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            input.Description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var ipResult = CoreTextPolicy.ValidateOptionalText(
            input.IpAddress,
            maxLength: 45,
            allowNewLines: false
        );
        var combined = Result.Combine(nameResult, descResult, ipResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        Name = nameResult.Value;
        NormalizedName = Name.ToUpperInvariant();
        Description = descResult.Value;
        DeviceTypeValueId = input.DeviceTypeValueId;
        IpAddress = ipResult.Value;
        NormalizedIpAddress = ipResult.Value?.ToUpperInvariant();
        IsActive = input.IsActive;
        LocationId = input.LocationId;
        PrintFrom = input.PrintFrom;
        PrintToOverride = input.PrintToOverride;
        PrinterDeviceId = input.PrinterDeviceId;
        ProxyDeviceId = input.ProxyDeviceId;
        HasCamera = input.HasCamera;
        CameraBarcodeConfigurationType = input.CameraBarcodeConfigurationType;
        KioskType = input.KioskType;

        if (input.AssignedLocationIds is not null)
        {
            _locations.Clear();
            foreach (var locId in input.AssignedLocationIds.Distinct())
            {
                if (locId != Domain.LocationId.Empty)
                {
                    _locations.Add(new DeviceLocation(Guid.CreateVersion7(), Id, locId));
                }
            }
        }

        return Result.Success();
    }
}
