using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Verification;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PersonViewed" /> audit record.</summary>
public readonly record struct PersonViewedId(Guid Value) : IStronglyTypedId<PersonViewedId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonViewedId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 person-viewed identifier.</summary>
    public static PersonViewedId Create()
    {
        return new PersonViewedId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonViewedId From(Guid value)
    {
        return new PersonViewedId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
