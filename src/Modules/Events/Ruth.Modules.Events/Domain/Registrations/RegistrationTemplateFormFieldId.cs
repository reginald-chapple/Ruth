using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationTemplateFormField.</summary>
public readonly record struct RegistrationTemplateFormFieldId(Guid Value)
    : IStronglyTypedId<RegistrationTemplateFormFieldId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationTemplateFormFieldId Empty => new(Guid.Empty);

    public static RegistrationTemplateFormFieldId Create()
    {
        return new RegistrationTemplateFormFieldId(StronglyTypedId.NextGuid());
    }

    public static RegistrationTemplateFormFieldId From(Guid value)
    {
        return new RegistrationTemplateFormFieldId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
