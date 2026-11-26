using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Modular payment gateway strategy contract supporting hosted tokenization, card/ACH charges, refunds, customer vaults, and recurring subscriptions.</summary>
public interface IPaymentGateway
{
    /// <summary>Gets the canonical lowercase provider key (for example, test, nmi, stripe).</summary>
    string ProviderKey { get; }

    /// <summary>Gets the human-readable provider display name.</summary>
    string DisplayName { get; }

    /// <summary>Exchanges a client-side hosted tokenizer token for a reusable customer vault token.</summary>
    Task<Result<GatewayCustomerVaultResult>> VaultPaymentMethodAsync(
        GatewayVaultRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Authorizes and captures a card or ACH charge.</summary>
    Task<Result<GatewayChargeResult>> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Issues a full or partial refund against an existing gateway transaction.</summary>
    Task<Result<GatewayRefundResult>> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Synchronizes a recurring billing schedule with the gateway.</summary>
    Task<Result<GatewaySubscriptionSyncResult>> SyncSubscriptionAsync(
        GatewaySubscriptionSyncRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Verifies webhook cryptographic signature and parses a normalized <see cref="GatewayWebhookPayload" />.</summary>
    Result<GatewayWebhookPayload> ParseAndVerifyWebhook(string rawBody, string? signatureHeader);
}
