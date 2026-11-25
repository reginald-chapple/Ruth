using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Join entity associating a physical <see cref="Device" /> with a served <see cref="Location" />.</summary>
public sealed class DeviceLocation : Entity<Guid>
{
    private DeviceLocation() { }

    internal DeviceLocation(Guid id, DeviceId deviceId, LocationId locationId)
        : base(id)
    {
        DeviceId = deviceId;
        LocationId = locationId;
    }

    /// <summary>Gets the owning device identifier.</summary>
    public DeviceId DeviceId { get; private set; }

    /// <summary>Gets the served location identifier.</summary>
    public LocationId LocationId { get; private set; }
}
