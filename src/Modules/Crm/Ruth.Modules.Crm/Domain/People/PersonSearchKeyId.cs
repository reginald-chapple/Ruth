using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PersonSearchKey" /> entity.</summary>
public readonly record struct PersonSearchKeyId(Guid Value) : IStronglyTypedId<PersonSearchKeyId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonSearchKeyId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 search key identifier.</summary>
    public static PersonSearchKeyId Create()
    {
        return new PersonSearchKeyId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonSearchKeyId From(Guid value)
    {
        return new PersonSearchKeyId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
