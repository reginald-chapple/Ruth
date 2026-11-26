using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Strongly-typed UUIDv7 identifier for a RegistrationRegistrantFeeSelection.</summary>
public readonly record struct RegistrationRegistrantFeeSelectionId(Guid Value)
    : IStronglyTypedId<RegistrationRegistrantFeeSelectionId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static RegistrationRegistrantFeeSelectionId Empty => new(Guid.Empty);

    public static RegistrationRegistrantFeeSelectionId Create()
    {
        return new RegistrationRegistrantFeeSelectionId(StronglyTypedId.NextGuid());
    }

    public static RegistrationRegistrantFeeSelectionId From(Guid value)
    {
        return new RegistrationRegistrantFeeSelectionId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
