using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationTemplateDiscount.</summary>
public readonly record struct RegistrationTemplateDiscountId(Guid Value)
    : IStronglyTypedId<RegistrationTemplateDiscountId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationTemplateDiscountId Empty => new(Guid.Empty);

    public static RegistrationTemplateDiscountId Create()
    {
        return new RegistrationTemplateDiscountId(StronglyTypedId.NextGuid());
    }

    public static RegistrationTemplateDiscountId From(Guid value)
    {
        return new RegistrationTemplateDiscountId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
