using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PersonPreviousName" /> entity.</summary>
public readonly record struct PersonPreviousNameId(Guid Value)
    : IStronglyTypedId<PersonPreviousNameId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonPreviousNameId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 previous name identifier.</summary>
    public static PersonPreviousNameId Create()
    {
        return new PersonPreviousNameId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonPreviousNameId From(Guid value)
    {
        return new PersonPreviousNameId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
