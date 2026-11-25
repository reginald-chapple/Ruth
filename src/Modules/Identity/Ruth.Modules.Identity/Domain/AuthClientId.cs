using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AuthClient" />.</summary>
public readonly record struct AuthClientId(Guid Value) : IStronglyTypedId<AuthClientId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static AuthClientId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 auth client identifier.</summary>
    public static AuthClientId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static AuthClientId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
