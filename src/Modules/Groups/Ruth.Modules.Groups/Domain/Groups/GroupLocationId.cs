using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupLocation" /> entity.</summary>
public readonly record struct GroupLocationId(Guid Value) : IStronglyTypedId<GroupLocationId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupLocationId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 GroupLocation identifier.</summary>
    public static GroupLocationId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupLocationId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
