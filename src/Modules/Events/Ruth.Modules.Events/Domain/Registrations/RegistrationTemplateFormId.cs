using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationTemplateForm.</summary>
public readonly record struct RegistrationTemplateFormId(Guid Value)
    : IStronglyTypedId<RegistrationTemplateFormId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationTemplateFormId Empty => new(Guid.Empty);

    public static RegistrationTemplateFormId Create()
    {
        return new RegistrationTemplateFormId(StronglyTypedId.NextGuid());
    }

    public static RegistrationTemplateFormId From(Guid value)
    {
        return new RegistrationTemplateFormId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
