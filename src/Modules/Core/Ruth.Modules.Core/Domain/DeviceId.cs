using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Device" />.</summary>
public readonly record struct DeviceId(Guid Value) : IStronglyTypedId<DeviceId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static DeviceId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 device identifier.</summary>
    public static DeviceId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static DeviceId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
