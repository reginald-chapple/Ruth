using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="UserAccount" />.</summary>
public readonly record struct UserAccountId(Guid Value) : IStronglyTypedId<UserAccountId>
{
    /// <inheritdoc />
    public static UserAccountId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static UserAccountId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static UserAccountId From(Guid value)
    {
        return new(value);
    }
}
