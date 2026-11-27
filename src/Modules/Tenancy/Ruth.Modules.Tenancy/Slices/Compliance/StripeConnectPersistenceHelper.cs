using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal static class StripeConnectPersistenceHelper
{
    public static Result UpsertAccountAndAttachTenant(
        TenancyDbContext dbContext,
        Tenant tenant,
        StripeConnectAccount? existingAccount,
        StripeConnectAccountStatus status,
        bool isMockAccount,
        string? onboardingUrl,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset nowUtc
    )
    {
        if (existingAccount is null)
        {
            var created = StripeConnectAccount.Create(
                tenant.Id,
                status.AccountId,
                isMockAccount,
                status.ChargesEnabled,
                status.PayoutsEnabled,
                status.DetailsSubmitted,
                onboardingUrl,
                expiresAtUtc,
                nowUtc
            );
            if (created.IsFailure)
            {
                return Result.Failure(created.Errors);
            }

            _ = dbContext.StripeConnectAccounts.Add(created.Value);
        }
        else
        {
            if (onboardingUrl is not null && expiresAtUtc.HasValue)
            {
                var recorded = existingAccount.RecordOnboardingSession(
                    status.AccountId,
                    onboardingUrl,
                    expiresAtUtc.Value,
                    isMockAccount,
                    nowUtc
                );
                if (recorded.IsFailure)
                {
                    return recorded;
                }
            }

            existingAccount.SynchronizeCapabilities(
                status.ChargesEnabled,
                status.PayoutsEnabled,
                status.DetailsSubmitted,
                nowUtc
            );
        }

        return tenant.AttachStripeConnectAccount(
            status.AccountId,
            status.ChargesEnabled,
            status.PayoutsEnabled
        );
    }
}
