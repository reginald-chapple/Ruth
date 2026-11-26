namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Evaluated pricing quote for a candidate service appointment, workshop, or resource rental.</summary>
/// <param name="TargetKind">Service or Resource target kind.</param>
/// <param name="TargetId">Resolved BookableService or RentableResource identifier.</param>
/// <param name="TargetName">Display name of the target.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="PricingKind">Resolved standard, member-free, member-discounted, punch-pass, or complimentary pricing kind.</param>
/// <param name="ListAmount">Standard undiscounted total amount.</param>
/// <param name="DiscountAmount">Member discount or waiver savings.</param>
/// <param name="TotalAmount">Payable monetary amount due (0 when member-free, punch-pass, or complimentary).</param>
/// <param name="PunchPassCreditsRequired">Credits deducted if settling with a punch pass (0 otherwise).</param>
/// <param name="AllowInvoiceBilling">Whether invoice billing is permitted for this booking.</param>
public sealed record BookingQuoteResponse(
    BookingTargetKind TargetKind,
    Guid TargetId,
    string TargetName,
    string CurrencyCode,
    BookingPricingKind PricingKind,
    decimal ListAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    int PunchPassCreditsRequired,
    bool AllowInvoiceBilling
);
