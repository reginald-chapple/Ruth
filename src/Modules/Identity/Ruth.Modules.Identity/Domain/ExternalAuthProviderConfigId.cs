using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="ExternalAuthProviderConfig" />.</summary>
public readonly record struct ExternalAuthProviderConfigId(Guid Value)
    : IStronglyTypedId<ExternalAuthProviderConfigId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static ExternalAuthProviderConfigId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 external provider identifier.</summary>
    public static ExternalAuthProviderConfigId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static ExternalAuthProviderConfigId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
