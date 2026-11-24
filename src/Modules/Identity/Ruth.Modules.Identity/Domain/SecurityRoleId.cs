using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SecurityRole" />.</summary>
public readonly record struct SecurityRoleId(Guid Value) : IStronglyTypedId<SecurityRoleId>
{
    /// <inheritdoc />
    public static SecurityRoleId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static SecurityRoleId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static SecurityRoleId From(Guid value)
    {
        return new(value);
    }
}
