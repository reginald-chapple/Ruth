using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationTemplateFee.</summary>
public readonly record struct RegistrationTemplateFeeId(Guid Value)
    : IStronglyTypedId<RegistrationTemplateFeeId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationTemplateFeeId Empty => new(Guid.Empty);

    public static RegistrationTemplateFeeId Create()
    {
        return new RegistrationTemplateFeeId(StronglyTypedId.NextGuid());
    }

    public static RegistrationTemplateFeeId From(Guid value)
    {
        return new RegistrationTemplateFeeId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
