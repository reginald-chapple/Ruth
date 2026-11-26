namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a selectable option item within a RegistrationFee.</summary>
public sealed record RegistrationFeeItemResponse(
    Guid Id,
    string Code,
    string Label,
    decimal UnitAmount,
    int? MaxQuantityAvailable,
    int DisplayOrder
);
