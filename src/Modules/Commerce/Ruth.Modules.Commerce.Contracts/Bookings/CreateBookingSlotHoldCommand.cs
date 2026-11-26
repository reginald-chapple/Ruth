using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Acquires a conflict-free TTL reservation hold on a candidate booking slot.</summary>
public sealed record CreateBookingSlotHoldCommand(
    BookingTargetKind TargetKind,
    string TargetIdOrSlug,
    DateTimeOffset SlotStartUtc,
    int? RequestedDurationMinutes = null,
    int Quantity = 1,
    Guid? ProviderPersonId = null,
    int HoldTtlMinutes = 10
) : ICommand<BookingSlotHoldResponse>;
