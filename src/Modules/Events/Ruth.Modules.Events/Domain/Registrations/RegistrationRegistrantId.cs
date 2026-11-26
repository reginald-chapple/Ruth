using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationRegistrant.</summary>
public readonly record struct RegistrationRegistrantId(Guid Value)
    : IStronglyTypedId<RegistrationRegistrantId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationRegistrantId Empty => new(Guid.Empty);

    public static RegistrationRegistrantId Create()
    {
        return new RegistrationRegistrantId(StronglyTypedId.NextGuid());
    }

    public static RegistrationRegistrantId From(Guid value)
    {
        return new RegistrationRegistrantId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
