using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationTemplatePlacement.</summary>
public readonly record struct RegistrationTemplatePlacementId(Guid Value)
    : IStronglyTypedId<RegistrationTemplatePlacementId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationTemplatePlacementId Empty => new(Guid.Empty);

    public static RegistrationTemplatePlacementId Create()
    {
        return new RegistrationTemplatePlacementId(StronglyTypedId.NextGuid());
    }

    public static RegistrationTemplatePlacementId From(Guid value)
    {
        return new RegistrationTemplatePlacementId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
