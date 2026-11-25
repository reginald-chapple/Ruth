using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="ExternalLoginLink" />.</summary>
public readonly record struct ExternalLoginLinkId(Guid Value)
    : IStronglyTypedId<ExternalLoginLinkId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static ExternalLoginLinkId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 external login link identifier.</summary>
    public static ExternalLoginLinkId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static ExternalLoginLinkId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
