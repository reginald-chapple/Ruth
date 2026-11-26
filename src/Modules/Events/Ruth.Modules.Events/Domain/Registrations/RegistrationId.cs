using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a Registration.</summary>
public readonly record struct RegistrationId(Guid Value) : IStronglyTypedId<RegistrationId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationId Empty => new(Guid.Empty);

    public static RegistrationId Create()
    {
        return new RegistrationId(StronglyTypedId.NextGuid());
    }

    public static RegistrationId From(Guid value)
    {
        return new RegistrationId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
