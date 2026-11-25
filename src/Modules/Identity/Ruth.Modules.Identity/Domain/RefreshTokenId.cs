using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="RefreshToken" />.</summary>
public readonly record struct RefreshTokenId(Guid Value) : IStronglyTypedId<RefreshTokenId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static RefreshTokenId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 refresh token identifier.</summary>
    public static RefreshTokenId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static RefreshTokenId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
