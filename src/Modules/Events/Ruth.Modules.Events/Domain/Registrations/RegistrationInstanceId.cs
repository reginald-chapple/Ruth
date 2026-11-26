using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationInstance.</summary>
public readonly record struct RegistrationInstanceId(Guid Value)
    : IStronglyTypedId<RegistrationInstanceId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationInstanceId Empty => new(Guid.Empty);

    public static RegistrationInstanceId Create()
    {
        return new RegistrationInstanceId(StronglyTypedId.NextGuid());
    }

    public static RegistrationInstanceId From(Guid value)
    {
        return new RegistrationInstanceId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
