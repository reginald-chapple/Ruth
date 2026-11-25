using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Contract or identifier type CheckInAreaLocationId in the CheckIn module.</summary>
public readonly record struct CheckInAreaLocationId(Guid Value)
    : IStronglyTypedId<CheckInAreaLocationId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static CheckInAreaLocationId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 area-location matrix cell identifier.</summary>
    public static CheckInAreaLocationId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static CheckInAreaLocationId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
