using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Category" />.</summary>
public readonly record struct CategoryId(Guid Value) : IStronglyTypedId<CategoryId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static CategoryId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 category identifier.</summary>
    public static CategoryId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static CategoryId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
