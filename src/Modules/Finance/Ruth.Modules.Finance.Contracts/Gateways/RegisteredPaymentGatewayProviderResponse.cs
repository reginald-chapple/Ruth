namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Metadata describing a compiled payment gateway implementation registered in the host.</summary>
/// <param name="ProviderKey">Canonical lowercase provider key.</param>
/// <param name="DisplayName">Human-readable provider title.</param>
public sealed record RegisteredPaymentGatewayProviderResponse(
    string ProviderKey,
    string DisplayName
);
