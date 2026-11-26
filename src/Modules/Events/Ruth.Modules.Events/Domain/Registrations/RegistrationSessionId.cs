using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationSession.</summary>
public readonly record struct RegistrationSessionId(Guid Value)
    : IStronglyTypedId<RegistrationSessionId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationSessionId Empty => new(Guid.Empty);

    public static RegistrationSessionId Create()
    {
        return new RegistrationSessionId(StronglyTypedId.NextGuid());
    }

    public static RegistrationSessionId From(Guid value)
    {
        return new RegistrationSessionId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
