using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationTemplateFeeItem.</summary>
public readonly record struct RegistrationTemplateFeeItemId(Guid Value)
    : IStronglyTypedId<RegistrationTemplateFeeItemId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationTemplateFeeItemId Empty => new(Guid.Empty);

    public static RegistrationTemplateFeeItemId Create()
    {
        return new RegistrationTemplateFeeItemId(StronglyTypedId.NextGuid());
    }

    public static RegistrationTemplateFeeItemId From(Guid value)
    {
        return new RegistrationTemplateFeeItemId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
