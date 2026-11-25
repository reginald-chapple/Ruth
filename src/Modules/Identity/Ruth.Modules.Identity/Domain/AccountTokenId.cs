using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AccountToken" />.</summary>
public readonly record struct AccountTokenId(Guid Value) : IStronglyTypedId<AccountTokenId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static AccountTokenId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 account token identifier.</summary>
    public static AccountTokenId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static AccountTokenId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
