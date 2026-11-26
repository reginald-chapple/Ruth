namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a persisted fee item selection on a RegistrationRegistrant.</summary>
public sealed record RegistrantFeeSelectionResponse(
    Guid FeeId,
    Guid FeeItemId,
    string Label,
    int Quantity,
    decimal UnitAmount,
    decimal LineTotal
);
