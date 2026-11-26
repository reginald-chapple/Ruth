namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Conflict-free bookable time slot returned by the slot engine.</summary>
/// <param name="SlotStartUtc">Scheduled appointment or rental start UTC timestamp.</param>
/// <param name="SlotEndUtc">Scheduled appointment or rental end UTC timestamp.</param>
/// <param name="EffectiveBlockStartUtc">Occupied resource/provider block start including setup buffer.</param>
/// <param name="EffectiveBlockEndUtc">Occupied resource/provider block end including teardown buffer.</param>
/// <param name="ProviderPersonId">Optional assigned provider person identifier.</param>
/// <param name="LocationId">Optional physical Core Location identifier.</param>
/// <param name="RemainingCapacity">Remaining concurrent seats or resource units available for this slot.</param>
public sealed record AvailableBookingSlotDto(
    DateTimeOffset SlotStartUtc,
    DateTimeOffset SlotEndUtc,
    DateTimeOffset EffectiveBlockStartUtc,
    DateTimeOffset EffectiveBlockEndUtc,
    Guid? ProviderPersonId,
    Guid? LocationId,
    int RemainingCapacity
);
