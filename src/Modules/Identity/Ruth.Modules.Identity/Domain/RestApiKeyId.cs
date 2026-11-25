using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="RestApiKey" />.</summary>
public readonly record struct RestApiKeyId(Guid Value) : IStronglyTypedId<RestApiKeyId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static RestApiKeyId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 REST API key identifier.</summary>
    public static RestApiKeyId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static RestApiKeyId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
