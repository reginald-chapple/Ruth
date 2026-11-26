using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationTemplate.</summary>
public readonly record struct RegistrationTemplateId(Guid Value)
    : IStronglyTypedId<RegistrationTemplateId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationTemplateId Empty => new(Guid.Empty);

    public static RegistrationTemplateId Create()
    {
        return new RegistrationTemplateId(StronglyTypedId.NextGuid());
    }

    public static RegistrationTemplateId From(Guid value)
    {
        return new RegistrationTemplateId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
