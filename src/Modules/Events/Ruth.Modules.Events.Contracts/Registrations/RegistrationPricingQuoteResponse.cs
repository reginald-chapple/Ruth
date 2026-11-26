namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Pricing and seat-allocation preview for a prospective Registration submission.</summary>
public sealed record RegistrationPricingQuoteResponse(
    Guid InstanceId,
    string CurrencyCode,
    int ConfirmedRegistrantCount,
    int WaitlistedRegistrantCount,
    decimal BaseSubtotal,
    decimal FeesSubtotal,
    decimal DiscountTotal,
    string? AppliedDiscountCode,
    decimal TotalAmount,
    decimal MinimumInitialAmountDue,
    RegistrationPaymentModel PaymentModel
);
