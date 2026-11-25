using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Location" />.</summary>
public readonly record struct LocationId(Guid Value) : IStronglyTypedId<LocationId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static LocationId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 location identifier.</summary>
    public static LocationId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static LocationId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
