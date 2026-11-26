using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Previews member-free vs. paid or punch-pass pricing for a candidate booking.</summary>
public sealed record PreviewBookingQuoteQuery(
    BookingTargetKind TargetKind,
    string TargetIdOrSlug,
    DateTimeOffset SlotStartUtc,
    int? RequestedDurationMinutes = null,
    int Quantity = 1,
    Guid? CustomerPersonId = null,
    string? ExplicitMembershipTier = null,
    Guid? RedeemPunchPassId = null
) : IQuery<BookingQuoteResponse>;
