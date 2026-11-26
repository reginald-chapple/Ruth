using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Infrastructure.Verification;

/// <summary>
/// Zero-config deterministic Stripe Connect Express provider enabling full nonprofit KYC and payout
/// onboarding verification in local development, demos, and automated tests without live Stripe keys.
/// </summary>
internal sealed class MockStripeConnectProvider(IOptions<TenancyOptions> options, IClock clock)
    : IStripeConnectProvider
{
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    private readonly ConcurrentDictionary<string, StripeConnectAccountStatus> _accounts = new(
        StringComparer.Ordinal
    );

    public bool IsMockMode => _options.StripeMockMode;

    public Task<StripeConnectSessionResult> CreateOrResumeOnboardingAsync(
        TenantId tenantId,
        string? existingAccountId,
        string legalName,
        string ein,
        string returnUrl,
        string refreshUrl,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ein);
        ArgumentException.ThrowIfNullOrWhiteSpace(returnUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshUrl);

        var accountId = string.IsNullOrWhiteSpace(existingAccountId)
            ? $"acct_mock_{tenantId.Value.ToString("N")[..16]}"
            : existingAccountId.Trim();

        _ = _accounts.GetOrAdd(accountId, static id => new StripeConnectAccountStatus(
            id,
            ChargesEnabled: false,
            PayoutsEnabled: false,
            DetailsSubmitted: false
        ));

        var separator = returnUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var onboardingUrl =
            $"{returnUrl}{separator}stripe_connect_mock={Uri.EscapeDataString(accountId)}";
        return Task.FromResult(
            new StripeConnectSessionResult(accountId, onboardingUrl, _clock.UtcNow.AddMinutes(30))
        );
    }

    public Task<StripeConnectAccountStatus> GetAccountStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var status = _accounts.GetOrAdd(
            accountId.Trim(),
            static id => new StripeConnectAccountStatus(
                id,
                ChargesEnabled: false,
                PayoutsEnabled: false,
                DetailsSubmitted: false
            )
        );
        return Task.FromResult(status);
    }

    public Task<StripeConnectAccountStatus> CompleteMockOnboardingAsync(
        string accountId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        if (!IsMockMode)
        {
            throw new InvalidOperationException(TenancyErrors.MockModeDisabled.Description);
        }

        var completed = new StripeConnectAccountStatus(
            accountId.Trim(),
            ChargesEnabled: true,
            PayoutsEnabled: true,
            DetailsSubmitted: true
        );
        _accounts[completed.AccountId] = completed;
        return Task.FromResult(completed);
    }
}
