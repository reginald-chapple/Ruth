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

internal sealed class StartStripeConnectOnboardingCommandHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    IStripeConnectProvider stripeConnectProvider,
    IClock clock
) : ICommandHandler<StartStripeConnectOnboardingCommand, StripeConnectOnboardingResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IStripeConnectProvider _stripeConnectProvider = stripeConnectProvider
        ?? throw new ArgumentNullException(nameof(stripeConnectProvider));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<StripeConnectOnboardingResponse>> HandleAsync(
        StartStripeConnectOnboardingCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var returnUrlResult = TenantSlugPolicy.NormalizeAndValidateOnboardingUrl(command.ReturnUrl);
        var refreshUrlResult = TenantSlugPolicy.NormalizeAndValidateOnboardingUrl(
            command.RefreshUrl
        );
        if (returnUrlResult.IsFailure || refreshUrlResult.IsFailure)
        {
            return TenancyErrors.InvalidOnboardingUrls;
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
        var session = await _stripeConnectProvider.CreateOrResumeOnboardingAsync(
            tenant.Id,
            stripeEntity?.ExternalAccountId ?? tenant.StripeConnectAccountId,
            tenant.LegalName,
            tenant.Ein,
            returnUrlResult.Value,
            refreshUrlResult.Value,
            cancellationToken
        );
        var accountStatus = await _stripeConnectProvider.GetAccountStatusAsync(
            session.AccountId,
            cancellationToken
        );

        var upsertResult = StripeConnectPersistenceHelper.UpsertAccountAndAttachTenant(
            _dbContext,
            tenant,
            stripeEntity,
            accountStatus,
            _stripeConnectProvider.IsMockMode,
            session.OnboardingUrl,
            session.ExpiresAtUtc,
            _clock.UtcNow
        );
        if (upsertResult.IsFailure)
        {
            return Result.Failure<StripeConnectOnboardingResponse>(upsertResult.Errors);
        }

        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        return MapSessionResponse(session, _stripeConnectProvider.IsMockMode);
    }

    private static StripeConnectOnboardingResponse MapSessionResponse(
        StripeConnectSessionResult session,
        bool isMockMode
    )
    {
        return new StripeConnectOnboardingResponse(
            session.AccountId,
            session.OnboardingUrl,
            isMockMode,
            session.ExpiresAtUtc
        );
    }
}
