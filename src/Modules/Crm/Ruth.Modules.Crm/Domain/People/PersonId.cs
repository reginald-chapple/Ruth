using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="Person" /> aggregate.</summary>
public readonly record struct PersonId(Guid Value) : IStronglyTypedId<PersonId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 person identifier.</summary>
    public static PersonId Create()
    {
        return new PersonId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonId From(Guid value)
    {
        return new PersonId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
