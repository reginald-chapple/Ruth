using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Confirms a conflict-free service appointment or resource rental, settles payment/invoice/punch-pass, emits ServiceBookingConfirmedIntegrationEvent, and syncs Wallet Passes.</summary>
public sealed record ConfirmBookingCommand(
    BookingTargetKind TargetKind,
    string TargetIdOrSlug,
    DateTimeOffset SlotStartUtc,
    string CustomerName,
    string CustomerEmail,
    int? RequestedDurationMinutes = null,
    int Quantity = 1,
    Guid? ProviderPersonId = null,
    string? HoldToken = null,
    Guid? CustomerPersonId = null,
    string? CustomerPhone = null,
    string? CustomerNotes = null,
    string? ExplicitMembershipTier = null,
    Guid? RedeemPunchPassId = null,
    bool RequestInvoiceBilling = false,
    Guid? FinancialGatewayId = null,
    string? OneTimeHostedPaymentToken = null,
    Guid? SavedPaymentMethodId = null,
    string? ExternalTransactionCode = null
) : ICommand<ServiceBookingResponse>;
