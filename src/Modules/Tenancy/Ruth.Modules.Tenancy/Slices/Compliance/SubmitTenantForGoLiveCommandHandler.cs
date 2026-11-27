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

internal sealed class SubmitTenantForGoLiveCommandHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    IIrsCharityVerificationProvider irsProvider,
    IStripeConnectProvider stripeProvider,
    IClock clock
) : ICommandHandler<SubmitTenantForGoLiveCommand, TenantComplianceStatusResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IIrsCharityVerificationProvider _irsProvider = irsProvider
        ?? throw new ArgumentNullException(nameof(irsProvider));
    private readonly IStripeConnectProvider _stripeProvider = stripeProvider
        ?? throw new ArgumentNullException(nameof(stripeProvider));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<TenantComplianceStatusResponse>> HandleAsync(
        SubmitTenantForGoLiveCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(
            x => x.Id == _tenantContext.TenantId,
            cancellationToken
        );
        if (tenant is null)
        {
            return TenancyErrors.TenantNotFound;
        }

        if (tenant.LifecycleStage != TenantLifecycleStage.Sandbox)
        {
            return TenancyErrors.InvalidLifecycleTransition;
        }

        var nowUtc = _clock.UtcNow;
        await SyncStripeCapabilitiesAsync(tenant, nowUtc, cancellationToken);
        await RecordIrsVerificationAsync(tenant, nowUtc, cancellationToken);

        var submitResult = tenant.SubmitForGoLive(nowUtc);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        if (submitResult.IsFailure)
        {
            return Result.Failure<TenantComplianceStatusResponse>(submitResult.Errors);
        }

        var updated = await TenantComplianceMapper.LoadStatusAsync(
            _dbContext,
            tenant.Id,
            _stripeProvider.IsMockMode,
            cancellationToken
        );
        return updated is not null ? updated : TenancyErrors.TenantNotFound;
    }

    private async Task SyncStripeCapabilitiesAsync(
        Tenant tenant,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var stripeEntity = await _dbContext.StripeConnectAccounts.FirstOrDefaultAsync(
            x => x.TargetTenantId == tenant.Id,
            cancellationToken
        );
        var accountId = stripeEntity?.ExternalAccountId ?? tenant.StripeConnectAccountId;
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return;
        }

        var stripeStatus =
            _stripeProvider.IsMockMode
            && stripeEntity is { IsMockAccount: true, ChargesEnabled: true, PayoutsEnabled: true }
                ? await _stripeProvider.CompleteMockOnboardingAsync(accountId, cancellationToken)
                : await _stripeProvider.GetAccountStatusAsync(accountId, cancellationToken);

        _ = StripeConnectPersistenceHelper.UpsertAccountAndAttachTenant(
            _dbContext,
            tenant,
            stripeEntity,
            stripeStatus,
            _stripeProvider.IsMockMode,
            onboardingUrl: null,
            expiresAtUtc: null,
            nowUtc
        );
    }

    private async Task RecordIrsVerificationAsync(
        Tenant tenant,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var irsResult = await _irsProvider.VerifyEinAsync(
            tenant.Ein,
            tenant.LegalName,
            tenant.StateCode,
            cancellationToken
        );
        tenant.RecordIrsVerification(
            irsResult.Status,
            irsResult.OfficialOrganizationName,
            irsResult.DeductibilityCode,
            nowUtc
        );

        var verificationRecord = IrsExemptVerification.Create(
            tenant.Id,
            irsResult.NormalizedEin,
            irsResult.Status,
            irsResult.OfficialOrganizationName,
            irsResult.SubsectionCode,
            irsResult.DeductibilityCode,
            irsResult.DiagnosticSummary,
            nowUtc
        );
        if (verificationRecord.IsSuccess)
        {
            _ = _dbContext.IrsExemptVerifications.Add(verificationRecord.Value);
        }
    }
}
