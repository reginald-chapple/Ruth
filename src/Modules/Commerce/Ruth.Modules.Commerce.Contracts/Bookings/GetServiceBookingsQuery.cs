using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Queries Coordinator Calendar and constituent bookings over an optional UTC window.</summary>
public sealed record GetServiceBookingsQuery(
    DateTimeOffset? RangeStartUtc = null,
    DateTimeOffset? RangeEndUtc = null,
    Guid? BookableServiceId = null,
    Guid? RentableResourceId = null,
    Guid? ProviderPersonId = null,
    Guid? CustomerPersonId = null,
    ServiceBookingStatus? Status = null
) : IQuery<IReadOnlyList<ServiceBookingResponse>>;
