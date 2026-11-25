using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Contract or identifier type CheckInConfigurationId in the CheckIn module.</summary>
public readonly record struct CheckInConfigurationId(Guid Value)
    : IStronglyTypedId<CheckInConfigurationId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static CheckInConfigurationId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 check-in configuration identifier.</summary>
    public static CheckInConfigurationId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static CheckInConfigurationId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
