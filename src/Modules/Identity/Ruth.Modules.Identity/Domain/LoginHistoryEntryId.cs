using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="LoginHistoryEntry" />.</summary>
public readonly record struct LoginHistoryEntryId(Guid Value)
    : IStronglyTypedId<LoginHistoryEntryId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static LoginHistoryEntryId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 login history entry identifier.</summary>
    public static LoginHistoryEntryId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static LoginHistoryEntryId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
