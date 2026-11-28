using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Domain;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Finance.Infrastructure.Gateways;

/// <summary>
/// Defense-in-depth decorator ensuring no registered <see cref="IPaymentGateway" /> executes
/// non-test live charges, vault operations, recurring subscriptions, or refunds unless the
/// active tenant workspace has reached <see cref="TenantLifecycleStage.Live" />.
/// </summary>
internal sealed class LiveTenantPaymentGatewayDecorator(IPaymentGateway inner) : IPaymentGateway
{
    public string ProviderKey => inner.ProviderKey;

    public string DisplayName => inner.DisplayName;

    public Task<Result<GatewayCustomerVaultResult>> VaultPaymentMethodAsync(
        GatewayVaultRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return RequiresLiveApproval(request.IsTestMode)
            ? Task.FromResult<Result<GatewayCustomerVaultResult>>(
                FinanceErrors.LiveChargeRequiresApprovedTenant
            )
            : inner.VaultPaymentMethodAsync(request, cancellationToken);
    }

    public Task<Result<GatewayChargeResult>> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return RequiresLiveApproval(request.IsTestMode)
            ? Task.FromResult<Result<GatewayChargeResult>>(
                FinanceErrors.LiveChargeRequiresApprovedTenant
            )
            : inner.ChargeAsync(request, cancellationToken);
    }

    public Task<Result<GatewayRefundResult>> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return RequiresLiveApproval(request.IsTestMode)
            ? Task.FromResult<Result<GatewayRefundResult>>(
                FinanceErrors.LiveChargeRequiresApprovedTenant
            )
            : inner.RefundAsync(request, cancellationToken);
    }

    public Task<Result<GatewaySubscriptionSyncResult>> SyncSubscriptionAsync(
        GatewaySubscriptionSyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return RequiresLiveApproval(request.IsTestMode)
            ? Task.FromResult<Result<GatewaySubscriptionSyncResult>>(
                FinanceErrors.LiveChargeRequiresApprovedTenant
            )
            : inner.SyncSubscriptionAsync(request, cancellationToken);
    }

    public Result<GatewayWebhookPayload> ParseAndVerifyWebhook(
        string rawBody,
        string? signatureHeader
    )
    {
        return inner.ParseAndVerifyWebhook(rawBody, signatureHeader);
    }

    private static bool RequiresLiveApproval(bool isTestMode)
    {
        return !isTestMode
            && AmbientTenantContext.Instance.HasTenant
            && !AmbientTenantContext.Instance.IsLive;
    }
}
