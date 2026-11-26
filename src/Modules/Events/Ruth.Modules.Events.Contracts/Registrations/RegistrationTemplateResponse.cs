namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Complete read projection for a RegistrationTemplate including forms, fees, discounts, and group placements.</summary>
public sealed record RegistrationTemplateResponse(
    Guid Id,
    string Name,
    Guid FinancialAccountId,
    Guid? CategoryId,
    Guid? DefaultFinancialGatewayId,
    string CurrencyCode,
    decimal BaseCostPerRegistrant,
    decimal MinimumInitialDepositPerRegistrant,
    RegistrationPaymentModel PaymentModel,
    bool WaitlistEnabled,
    int SeatHoldTtlMinutes,
    int MaxRegistrantsPerSubmission,
    bool IsActive,
    IReadOnlyList<RegistrationFormResponse> Forms,
    IReadOnlyList<RegistrationFeeResponse> Fees,
    IReadOnlyList<RegistrationDiscountResponse> Discounts,
    IReadOnlyList<RegistrationGroupPlacementResponse> GroupPlacements,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
