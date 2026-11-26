namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Normalized availability or blackout interval used by <see cref="BookingSlotEngine" />.</summary>
public sealed record NormalizedAvailabilityInterval(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    Guid? ProviderPersonId,
    Guid? LocationId
);
