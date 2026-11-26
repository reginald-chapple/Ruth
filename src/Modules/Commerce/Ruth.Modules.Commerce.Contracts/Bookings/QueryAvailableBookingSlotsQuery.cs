using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Evaluates conflict-free available slots for a service or rentable resource over a UTC range.</summary>
/// <param name="TargetKind">Service or Resource target kind.</param>
/// <param name="TargetIdOrSlug">Target BookableService or RentableResource GUID or slug.</param>
/// <param name="RangeStartUtc">Inclusive UTC range start.</param>
/// <param name="RangeEndUtc">Exclusive UTC range end.</param>
/// <param name="RequestedDurationMinutes">Optional requested rental duration in minutes (required or defaulted for resources).</param>
/// <param name="RequestedQuantity">Participants or resource units requested.</param>
/// <param name="ProviderPersonId">Optional specific provider filter for services.</param>
public sealed record QueryAvailableBookingSlotsQuery(
    BookingTargetKind TargetKind,
    string TargetIdOrSlug,
    DateTimeOffset RangeStartUtc,
    DateTimeOffset RangeEndUtc,
    int? RequestedDurationMinutes = null,
    int RequestedQuantity = 1,
    Guid? ProviderPersonId = null
) : IQuery<IReadOnlyList<AvailableBookingSlotDto>>;
