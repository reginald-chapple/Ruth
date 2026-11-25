using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PhoneNumber" /> entity.</summary>
public readonly record struct PhoneNumberId(Guid Value) : IStronglyTypedId<PhoneNumberId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PhoneNumberId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 phone number identifier.</summary>
    public static PhoneNumberId Create()
    {
        return new PhoneNumberId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PhoneNumberId From(Guid value)
    {
        return new PhoneNumberId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
