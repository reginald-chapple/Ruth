using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Commerce.Contracts.Events;

/// <summary>Outbox integration event emitted when a bookable service appointment, workshop seat, or facility/resource rental is confirmed.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when confirmed.</param>
/// <param name="BookingId">Confirmed booking identifier.</param>
/// <param name="ConfirmationCode">Human-readable booking confirmation code.</param>
/// <param name="TargetKind">Whether the booking targets a service or rentable resource.</param>
/// <param name="BookableServiceId">Optional bookable service identifier.</param>
/// <param name="RentableResourceId">Optional rentable resource identifier.</param>
/// <param name="TargetName">Display name of the booked service or resource.</param>
/// <param name="DeliveryMode">In-person, virtual room, or hybrid delivery mode.</param>
/// <param name="RequiresVirtualRoom">True when VirtualRooms should provision an embedded LiveKit session.</param>
/// <param name="VirtualRoomReferenceKey">Deterministic room correlation key for virtual room provisioning.</param>
/// <param name="CustomerPersonId">Optional customer CRM person identifier.</param>
/// <param name="CustomerName">Customer full name.</param>
/// <param name="CustomerEmail">Customer email address.</param>
/// <param name="ProviderPersonId">Optional assigned staff/coach/provider person identifier.</param>
/// <param name="LocationId">Optional physical Core Location identifier.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="StartUtc">Scheduled booking start UTC timestamp.</param>
/// <param name="EndUtc">Scheduled booking end UTC timestamp.</param>
/// <param name="ParticipantOrUnitCount">Booked participant or equipment unit count.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="TotalAmount">Total booking price after member rules.</param>
/// <param name="PaidAmount">Amount settled at confirmation.</param>
/// <param name="LinkedInvoiceId">Optional linked Finance invoice identifier.</param>
public sealed record ServiceBookingConfirmedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid BookingId,
    string ConfirmationCode,
    BookingTargetKind TargetKind,
    Guid? BookableServiceId,
    Guid? RentableResourceId,
    string TargetName,
    BookingDeliveryMode DeliveryMode,
    bool RequiresVirtualRoom,
    string? VirtualRoomReferenceKey,
    Guid? CustomerPersonId,
    string CustomerName,
    string CustomerEmail,
    Guid? ProviderPersonId,
    Guid? LocationId,
    Guid? BranchId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int ParticipantOrUnitCount,
    string CurrencyCode,
    decimal TotalAmount,
    decimal PaidAmount,
    Guid? LinkedInvoiceId
) : IIntegrationEvent;
