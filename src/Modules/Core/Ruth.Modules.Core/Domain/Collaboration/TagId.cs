using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Tag" />.</summary>
public readonly record struct TagId(Guid Value) : IStronglyTypedId<TagId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static TagId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 tag identifier.</summary>
    public static TagId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static TagId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
