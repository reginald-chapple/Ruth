using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Completes simulated Stripe Connect KYC and payout readiness for the active tenant when running in mock/demo mode.</summary>
public sealed record CompleteMockStripeConnectCommand : ICommand<TenantComplianceStatusResponse>;
