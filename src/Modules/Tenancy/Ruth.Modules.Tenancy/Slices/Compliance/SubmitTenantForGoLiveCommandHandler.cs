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

        var nowUtc = _clock.UtcNow;
        await SyncStripeAndRecordIrsAsync(tenant, nowUtc, cancellationToken);

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

    private async Task SyncStripeAndRecordIrsAsync(
        Tenant tenant,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(tenant.StripeConnectAccountId))
        {
            var stripeStatus = await _stripeProvider.GetAccountStatusAsync(
                tenant.StripeConnectAccountId,
                cancellationToken
            );
            tenant.AttachStripeConnectAccount(
                stripeStatus.AccountId,
                stripeStatus.ChargesEnabled,
                stripeStatus.PayoutsEnabled
            );
        }

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
        _ = _dbContext.IrsVerificationRecords.Add(
            IrsExemptVerificationRecord.Create(
                tenant.Id,
                irsResult.NormalizedEin,
                irsResult.Status,
                irsResult.OfficialOrganizationName,
                irsResult.SubsectionCode,
                irsResult.DeductibilityCode,
                irsResult.DiagnosticSummary,
                nowUtc
            )
        );
    }
}
