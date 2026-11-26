namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Result returned when vaulting a hosted payment token with a payment gateway.</summary>
/// <param name="CustomerVaultToken">Opaque reusable gateway customer vault token.</param>
/// <param name="MaskedAccountNumber">Masked account or card suffix (for example, ****4242).</param>
/// <param name="CardBrand">Normalized card brand or bank label.</param>
/// <param name="ExpiryMonth">Optional card expiration month (1..12).</param>
/// <param name="ExpiryYear">Optional four-digit card expiration year.</param>
public sealed record GatewayCustomerVaultResult(
    string CustomerVaultToken,
    string MaskedAccountNumber,
    string? CardBrand,
    int? ExpiryMonth,
    int? ExpiryYear
);
