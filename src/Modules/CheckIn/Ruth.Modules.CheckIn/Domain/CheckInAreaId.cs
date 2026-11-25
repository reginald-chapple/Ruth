using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Contract or identifier type CheckInAreaId in the CheckIn module.</summary>
public readonly record struct CheckInAreaId(Guid Value) : IStronglyTypedId<CheckInAreaId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static CheckInAreaId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 check-in area identifier.</summary>
    public static CheckInAreaId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static CheckInAreaId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
