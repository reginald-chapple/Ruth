using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Cancels a service or resource booking (enforcing notice window unless <paramref name="StaffOverride" /> is true), optionally refunds/voids via Finance, and updates Wallet Passes.</summary>
public sealed record CancelBookingCommand(
    Guid BookingId,
    string Reason,
    bool StaffOverride = false,
    bool IssueRefundOrVoidInvoice = true,
    Guid? ProcessedByPersonId = null
) : ICommand<ServiceBookingResponse>;
