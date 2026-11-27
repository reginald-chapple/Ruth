using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal sealed class CompleteMockStripeConnectCommandHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    IStripeConnectProvider stripeConnectProvider,
    IClock clock
) : ICommandHandler<CompleteMockStripeConnectCommand, TenantComplianceStatusResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IStripeConnectProvider _stripeConnectProvider = stripeConnectProvider
        ?? throw new ArgumentNullException(nameof(stripeConnectProvider));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<TenantComplianceStatusResponse>> HandleAsync(
        CompleteMockStripeConnectCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_stripeConnectProvider.IsMockMode)
        {
            return TenancyErrors.MockModeDisabled;
        }

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(
            x => x.Id == _tenantContext.TenantId,
            cancellationToken
        );
        if (tenant is null)
        {
            return TenancyErrors.TenantNotFound;
        }

        var stripeEntity = await _dbContext.StripeConnectAccounts.FirstOrDefaultAsync(
            x => x.TargetTenantId == tenant.Id,
            cancellationToken
        );
        var (accountId, onboardingUrl, expiresAtUtc) = await EnsureAccountSessionAsync(
            tenant,
            stripeEntity,
            cancellationToken
        );

        var completed = await _stripeConnectProvider.CompleteMockOnboardingAsync(
            accountId,
            cancellationToken
        );
        var upsertResult = StripeConnectPersistenceHelper.UpsertAccountAndAttachTenant(
            _dbContext,
            tenant,
            stripeEntity,
            completed,
            isMockAccount: true,
            onboardingUrl,
            expiresAtUtc,
            _clock.UtcNow
        );
        if (upsertResult.IsFailure)
        {
            return Result.Failure<TenantComplianceStatusResponse>(upsertResult.Errors);
        }

        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        var status = await TenantComplianceMapper.LoadStatusAsync(
            _dbContext,
            tenant.Id,
            _stripeConnectProvider.IsMockMode,
            cancellationToken
        );
        return status is not null ? status : TenancyErrors.TenantNotFound;
    }

    private async Task<(
        string AccountId,
        string? Url,
        DateTimeOffset? ExpiresAtUtc
    )> EnsureAccountSessionAsync(
        Tenant tenant,
        StripeConnectAccount? stripeEntity,
        CancellationToken cancellationToken
    )
    {
        var existingId = stripeEntity?.ExternalAccountId ?? tenant.StripeConnectAccountId;
        if (!string.IsNullOrWhiteSpace(existingId))
        {
            return (
                existingId,
                stripeEntity?.LastOnboardingUrl,
                stripeEntity?.OnboardingExpiresAtUtc
            );
        }

        var session = await _stripeConnectProvider.CreateOrResumeOnboardingAsync(
            tenant.Id,
            existingAccountId: null,
            tenant.LegalName,
            tenant.Ein,
            "/admin/compliance",
            "/admin/compliance",
            cancellationToken
        );
        return (session.AccountId, session.OnboardingUrl, session.ExpiresAtUtc);
    }
}
