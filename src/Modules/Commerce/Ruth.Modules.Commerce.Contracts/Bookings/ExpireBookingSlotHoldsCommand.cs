using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Releases all expired TTL booking slot holds.</summary>
public sealed record ExpireBookingSlotHoldsCommand : ICommand<BookingSlotHoldCleanupResponse>;
