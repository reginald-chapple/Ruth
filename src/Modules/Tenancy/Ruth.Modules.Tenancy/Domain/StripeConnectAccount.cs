using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain;

/// <summary>Persisted Stripe Connect Express onboarding &amp; payout readiness account bound to a <see cref="Tenant" />.</summary>
public sealed class StripeConnectAccount
    : AggregateRoot<StripeConnectAccountId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private StripeConnectAccount() { }

    private StripeConnectAccount(
        StripeConnectAccountId id,
        TenantId targetTenantId,
        string externalAccountId,
        bool isMockAccount,
        bool chargesEnabled,
        bool payoutsEnabled,
        bool detailsSubmitted,
        string? lastOnboardingUrl,
        DateTimeOffset? onboardingExpiresAtUtc,
        DateTimeOffset nowUtc
    )
        : base(id)
    {
        TargetTenantId = targetTenantId;
        ExternalAccountId = externalAccountId;
        IsMockAccount = isMockAccount;
        ChargesEnabled = chargesEnabled;
        PayoutsEnabled = payoutsEnabled;
        DetailsSubmitted = detailsSubmitted;
        LastOnboardingUrl = lastOnboardingUrl;
        OnboardingExpiresAtUtc = onboardingExpiresAtUtc?.ToUniversalTime();
        LastSyncedOnUtc = nowUtc.ToUniversalTime();
        CreatedOnUtc = nowUtc.ToUniversalTime();
    }

    public TenantId TargetTenantId { get; private set; }

    public string ExternalAccountId { get; private set; } = string.Empty;

    public bool IsMockAccount { get; private set; }

    public bool ChargesEnabled { get; private set; }

    public bool PayoutsEnabled { get; private set; }

    public bool DetailsSubmitted { get; private set; }

    public string? LastOnboardingUrl { get; private set; }

    public DateTimeOffset? OnboardingExpiresAtUtc { get; private set; }

    public DateTimeOffset LastSyncedOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public Guid ConcurrencyStamp { get; private set; }

    public static Result<StripeConnectAccount> Create(
        TenantId targetTenantId,
        string? externalAccountId,
        bool isMockAccount,
        bool chargesEnabled,
        bool payoutsEnabled,
        bool detailsSubmitted,
        string? onboardingUrl,
        DateTimeOffset? onboardingExpiresAtUtc,
        DateTimeOffset nowUtc
    )
    {
        return targetTenantId.IsEmpty ? TenancyErrors.TenantNotFound
            : string.IsNullOrWhiteSpace(externalAccountId) || externalAccountId.Trim().Length > 128
                ? TenancyErrors.InvalidStripeAccountId
            : new StripeConnectAccount(
                StripeConnectAccountId.Create(),
                targetTenantId,
                externalAccountId.Trim(),
                isMockAccount,
                chargesEnabled,
                payoutsEnabled,
                detailsSubmitted,
                string.IsNullOrWhiteSpace(onboardingUrl) ? null : onboardingUrl.Trim(),
                onboardingExpiresAtUtc,
                nowUtc
            );
    }

    public Result RecordOnboardingSession(
        string? externalAccountId,
        string? onboardingUrl,
        DateTimeOffset expiresAtUtc,
        bool isMockAccount,
        DateTimeOffset nowUtc
    )
    {
        if (string.IsNullOrWhiteSpace(externalAccountId) || externalAccountId.Trim().Length > 128)
        {
            return TenancyErrors.InvalidStripeAccountId;
        }

        ExternalAccountId = externalAccountId.Trim();
        IsMockAccount = isMockAccount;
        LastOnboardingUrl = string.IsNullOrWhiteSpace(onboardingUrl) ? null : onboardingUrl.Trim();
        OnboardingExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        LastSyncedOnUtc = nowUtc.ToUniversalTime();
        return Result.Success();
    }

    public void SynchronizeCapabilities(
        bool chargesEnabled,
        bool payoutsEnabled,
        bool detailsSubmitted,
        DateTimeOffset nowUtc
    )
    {
        ChargesEnabled = chargesEnabled;
        PayoutsEnabled = payoutsEnabled;
        DetailsSubmitted = detailsSubmitted;
        LastSyncedOnUtc = nowUtc.ToUniversalTime();
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
