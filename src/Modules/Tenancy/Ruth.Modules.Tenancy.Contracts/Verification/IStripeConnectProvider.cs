using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Verification;

/// <summary>Abstraction for Stripe Connect Express nonprofit onboarding and payout readiness checks.</summary>
public interface IStripeConnectProvider
{
    /// <summary>Gets whether this provider is operating in deterministic zero-config mock/demo mode.</summary>
    bool IsMockMode { get; }

    /// <summary>Creates or resumes an Express onboarding link for the given organization.</summary>
    Task<StripeConnectSessionResult> CreateOrResumeOnboardingAsync(
        TenantId tenantId,
        string? existingAccountId,
        string legalName,
        string ein,
        string returnUrl,
        string refreshUrl,
        CancellationToken cancellationToken = default
    );

    /// <summary>Retrieves the latest capability status for <paramref name="accountId" />.</summary>
    Task<StripeConnectAccountStatus> GetAccountStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Completes simulated KYC and enables charges/payouts when <see cref="IsMockMode" /> is <see langword="true" />.</summary>
    Task<StripeConnectAccountStatus> CompleteMockOnboardingAsync(
        string accountId,
        CancellationToken cancellationToken = default
    );
}
