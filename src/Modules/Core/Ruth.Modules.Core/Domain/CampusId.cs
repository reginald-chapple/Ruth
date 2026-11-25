using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Campus" />.</summary>
public readonly record struct CampusId(Guid Value) : IStronglyTypedId<CampusId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static CampusId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 campus identifier.</summary>
    public static CampusId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static CampusId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
