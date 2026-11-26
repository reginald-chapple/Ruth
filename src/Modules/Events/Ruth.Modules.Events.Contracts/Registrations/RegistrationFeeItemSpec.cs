namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for a selectable option item within a RegistrationFee.</summary>
public sealed record RegistrationFeeItemSpec(
    string Code,
    string Label,
    decimal UnitAmount,
    int? MaxQuantityAvailable = null,
    int DisplayOrder = 0
);
