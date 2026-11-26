namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Read projection representing an active TTL reservation hold on a booking slot.</summary>
public sealed record BookingSlotHoldResponse(
    Guid Id,
    string HoldToken,
    BookingTargetKind TargetKind,
    Guid? BookableServiceId,
    Guid? RentableResourceId,
    Guid? ProviderPersonId,
    Guid? LocationId,
    DateTimeOffset SlotStartUtc,
    DateTimeOffset SlotEndUtc,
    DateTimeOffset EffectiveBlockStartUtc,
    DateTimeOffset EffectiveBlockEndUtc,
    int Quantity,
    DateTimeOffset ExpiresAtUtc
);
