using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationPaymentRecord.</summary>
public readonly record struct RegistrationPaymentRecordId(Guid Value)
    : IStronglyTypedId<RegistrationPaymentRecordId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationPaymentRecordId Empty => new(Guid.Empty);

    public static RegistrationPaymentRecordId Create()
    {
        return new RegistrationPaymentRecordId(StronglyTypedId.NextGuid());
    }

    public static RegistrationPaymentRecordId From(Guid value)
    {
        return new RegistrationPaymentRecordId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
