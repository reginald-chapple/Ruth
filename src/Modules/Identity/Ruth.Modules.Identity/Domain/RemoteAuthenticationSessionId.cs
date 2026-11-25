using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="RemoteAuthenticationSession" />.</summary>
public readonly record struct RemoteAuthenticationSessionId(Guid Value)
    : IStronglyTypedId<RemoteAuthenticationSessionId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static RemoteAuthenticationSessionId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 remote authentication session identifier.</summary>
    public static RemoteAuthenticationSessionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static RemoteAuthenticationSessionId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
