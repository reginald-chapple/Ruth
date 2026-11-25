using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="EntitySet" />.</summary>
public readonly record struct EntitySetId(Guid Value) : IStronglyTypedId<EntitySetId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static EntitySetId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 entity set identifier.</summary>
    public static EntitySetId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static EntitySetId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
