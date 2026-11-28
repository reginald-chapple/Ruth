using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>
/// Processes an HMAC-verified, replay-checked Stripe Connect webhook event on the platform control plane.
/// </summary>
public sealed record ProcessStripeConnectWebhookCommand(
    string RawPayload,
    string? StripeSignatureHeader
) : ICommand<StripeConnectWebhookResponse>, IPlatformControlPlaneRequest;
