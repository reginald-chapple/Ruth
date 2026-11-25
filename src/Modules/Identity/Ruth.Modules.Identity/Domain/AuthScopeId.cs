using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AuthScope" />.</summary>
public readonly record struct AuthScopeId(Guid Value) : IStronglyTypedId<AuthScopeId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static AuthScopeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 auth scope identifier.</summary>
    public static AuthScopeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static AuthScopeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
