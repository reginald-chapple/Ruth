using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PersonRedirect" /> ledger entry.</summary>
public readonly record struct PersonRedirectId(Guid Value) : IStronglyTypedId<PersonRedirectId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonRedirectId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 redirect identifier.</summary>
    public static PersonRedirectId Create()
    {
        return new PersonRedirectId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonRedirectId From(Guid value)
    {
        return new PersonRedirectId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
