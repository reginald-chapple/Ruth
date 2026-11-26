namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Selected fee item and quantity for an individual registrant.</summary>
public sealed record RegistrantFeeSelectionInputDto(Guid FeeId, Guid FeeItemId, int Quantity = 1);
