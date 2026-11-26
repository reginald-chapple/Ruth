using Ruth.Modules.Commerce.Contracts.Bookings;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Normalized active reservation or TTL hold interval used for conflict-free capacity calculation.</summary>
public sealed record OccupiedBookingBlock(
    BookingTargetKind TargetKind,
    BookableServiceId? BookableServiceId,
    RentableResourceId? RentableResourceId,
    Guid? ProviderPersonId,
    Guid? LocationId,
    DateTimeOffset SlotStartUtc,
    DateTimeOffset SlotEndUtc,
    DateTimeOffset EffectiveBlockStartUtc,
    DateTimeOffset EffectiveBlockEndUtc,
    int Quantity
);
