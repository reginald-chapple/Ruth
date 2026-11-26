using Ruth.Modules.Finance.Contracts.Gateways;

namespace Ruth.Modules.Finance.Contracts.PaymentMethods;

/// <summary>Read projection for a tokenized constituent or organization saved payment method.</summary>
/// <param name="Id">Saved payment method identifier.</param>
/// <param name="PersonId">Owner constituent or Business organization Person identifier.</param>
/// <param name="HouseholdId">Optional shared Household identifier.</param>
/// <param name="FinancialGatewayId">Gateway configuration identifier owning the vault token.</param>
/// <param name="GatewayName">Display name of the gateway.</param>
/// <param name="MethodKind">Payment instrument kind.</param>
/// <param name="DisplayName">User-friendly payment method nickname.</param>
/// <param name="CardBrand">Optional card brand or bank label.</param>
/// <param name="AccountNumberMasked">Masked suffix (for example, ****4242).</param>
/// <param name="ExpiryMonth">Optional card expiration month (1..12).</param>
/// <param name="ExpiryYear">Optional four-digit card expiration year.</param>
/// <param name="IsDefault">Whether this is the constituent's default payment method.</param>
/// <param name="IsActive">Whether this vaulted method is active.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record SavedPaymentMethodResponse(
    Guid Id,
    Guid PersonId,
    Guid? HouseholdId,
    Guid FinancialGatewayId,
    string GatewayName,
    PaymentMethodKind MethodKind,
    string DisplayName,
    string? CardBrand,
    string AccountNumberMasked,
    int? ExpiryMonth,
    int? ExpiryYear,
    bool IsDefault,
    bool IsActive,
    DateTimeOffset CreatedOnUtc
);
