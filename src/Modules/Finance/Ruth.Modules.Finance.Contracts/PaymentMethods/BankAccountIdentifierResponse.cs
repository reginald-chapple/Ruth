namespace Ruth.Modules.Finance.Contracts.PaymentMethods;

/// <summary>Read projection for a hashed bank routing/account identifier used in check-scanner and ACH matching.</summary>
/// <param name="Id">Unique bank account identifier record ID.</param>
/// <param name="PersonId">Matched constituent or Business organization Person identifier.</param>
/// <param name="MaskedRoutingNumber">Masked routing suffix (for example, *****6789).</param>
/// <param name="MaskedAccountNumber">Masked account suffix (for example, ****1234).</param>
/// <param name="BankName">Optional financial institution name.</param>
/// <param name="RoutingAndAccountHash">Keyed HMAC-SHA256 digest used for O(1) lookup.</param>
/// <param name="VerifiedOnUtc">UTC timestamp when this routing/account link was verified.</param>
public sealed record BankAccountIdentifierResponse(
    Guid Id,
    Guid PersonId,
    string MaskedRoutingNumber,
    string MaskedAccountNumber,
    string? BankName,
    string RoutingAndAccountHash,
    DateTimeOffset VerifiedOnUtc
);
