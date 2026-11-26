namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for a fee group on a RegistrationTemplate.</summary>
public sealed record RegistrationFeeSpec(
    string Name,
    RegistrationFeeType FeeType,
    bool IsRequired = false,
    bool AllowDiscount = true,
    int DisplayOrder = 0,
    IReadOnlyList<RegistrationFeeItemSpec>? Items = null
);
