using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="OrganizationProfile" />.</summary>
public readonly record struct OrganizationId(Guid Value) : IStronglyTypedId<OrganizationId>
{
    /// <inheritdoc />
    public static OrganizationId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static OrganizationId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static OrganizationId From(Guid value)
    {
        return new(value);
    }
}
