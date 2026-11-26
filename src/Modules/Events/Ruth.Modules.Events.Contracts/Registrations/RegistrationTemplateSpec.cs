namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for creating or updating a RegistrationTemplate.</summary>
public sealed record RegistrationTemplateSpec(
    string Name,
    Guid FinancialAccountId,
    Guid? CategoryId = null,
    Guid? DefaultFinancialGatewayId = null,
    string CurrencyCode = "USD",
    decimal BaseCostPerRegistrant = 0m,
    decimal MinimumInitialDepositPerRegistrant = 0m,
    RegistrationPaymentModel PaymentModel = RegistrationPaymentModel.PayInFull,
    bool WaitlistEnabled = true,
    int SeatHoldTtlMinutes = 15,
    int MaxRegistrantsPerSubmission = 10,
    bool IsActive = true,
    IReadOnlyList<RegistrationFormSpec>? Forms = null,
    IReadOnlyList<RegistrationFeeSpec>? Fees = null,
    IReadOnlyList<RegistrationDiscountSpec>? Discounts = null,
    IReadOnlyList<RegistrationGroupPlacementSpec>? GroupPlacements = null
);
