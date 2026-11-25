using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PersonDuplicate" /> record.</summary>
public readonly record struct PersonDuplicateId(Guid Value) : IStronglyTypedId<PersonDuplicateId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonDuplicateId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 duplicate identifier.</summary>
    public static PersonDuplicateId Create()
    {
        return new PersonDuplicateId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonDuplicateId From(Guid value)
    {
        return new PersonDuplicateId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
