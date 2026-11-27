using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.Modules.Tenancy.Slices.Compliance;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Slices.Operators;

internal sealed class ReviewTenantGoLiveCommandHandler(
    TenancyDbContext dbContext,
    ITenantHostResolver hostResolver,
    ActiveTenantRegistry activeTenantRegistry,
    IStripeConnectProvider stripeProvider,
    IClock clock
) : ICommandHandler<ReviewTenantGoLiveCommand, TenantComplianceStatusResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantHostResolver _hostResolver = hostResolver
        ?? throw new ArgumentNullException(nameof(hostResolver));
    private readonly ActiveTenantRegistry _activeTenantRegistry = activeTenantRegistry
        ?? throw new ArgumentNullException(nameof(activeTenantRegistry));
    private readonly IStripeConnectProvider _stripeProvider = stripeProvider
        ?? throw new ArgumentNullException(nameof(stripeProvider));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<TenantComplianceStatusResponse>> HandleAsync(
        ReviewTenantGoLiveCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.OperatorEmail))
        {
            return TenancyErrors.InvalidOperatorIdentity;
        }

        var normalizedOperator = command.OperatorEmail.Trim().ToLowerInvariant();
        var activeOperatorExists = await _dbContext
            .PlatformOperators.AsNoTracking()
            .AnyAsync(
                x => x.EmailNormalized == normalizedOperator && x.IsActive,
                cancellationToken
            );
        if (!activeOperatorExists)
        {
            return TenancyErrors.InvalidOperatorIdentity;
        }

        var tenantId = TenantId.From(command.TargetTenantId);
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(
            x => x.Id == tenantId,
            cancellationToken
        );
        if (tenant is null)
        {
            return TenancyErrors.TenantNotFound;
        }

        var domains = await _dbContext
            .TenantDomains.Where(x => x.TargetTenantId == tenantId)
            .ToListAsync(cancellationToken);

        var decisionResult = ApplyDecision(command, normalizedOperator, tenant, domains);
        if (decisionResult.IsFailure)
        {
            return Result.Failure<TenantComplianceStatusResponse>(decisionResult.Errors);
        }

        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        _activeTenantRegistry.Invalidate();
        foreach (var domain in domains)
        {
            _hostResolver.InvalidateHost(domain.Hostname);
        }

        return TenantComplianceMapper.Map(tenant, domains, _stripeProvider.IsMockMode);
    }

    private Result ApplyDecision(
        ReviewTenantGoLiveCommand command,
        string normalizedOperator,
        Tenant tenant,
        List<TenantDomain> domains
    )
    {
        var nowUtc = _clock.UtcNow;
        switch (command.Decision)
        {
            case OperatorReviewDecision.ApproveLive:
                var approveResult = tenant.ApproveLive(
                    normalizedOperator,
                    command.Notes,
                    command.ManualIrsOverrideApproved,
                    nowUtc
                );
                if (approveResult.IsSuccess)
                {
                    PromoteVerifiedCustomDomainIfRequested(domains);
                }

                return approveResult;

            case OperatorReviewDecision.ReturnToSandbox:
                var returnResult = tenant.ReturnToSandbox(
                    normalizedOperator,
                    command.Notes,
                    nowUtc
                );
                if (returnResult.IsSuccess)
                {
                    RestoreSandboxDomainPrimary(domains);
                }

                return returnResult;

            case OperatorReviewDecision.Suspend:
                return tenant.Suspend(normalizedOperator, command.Notes, nowUtc);

            default:
                return DomainError.Validation(
                    "Tenancy.InvalidDecision",
                    "Unsupported operator review decision."
                );
        }
    }

    private static void PromoteVerifiedCustomDomainIfRequested(List<TenantDomain> domains)
    {
        var verifiedCustom = domains.FirstOrDefault(static d =>
            d.IsCustomDomain && d.IsVerified && d.MakePrimaryWhenVerified
        );
        if (verifiedCustom is null)
        {
            return;
        }

        foreach (var domain in domains)
        {
            domain.SetPrimary(domain.Id == verifiedCustom.Id);
        }
    }

    private static void RestoreSandboxDomainPrimary(List<TenantDomain> domains)
    {
        var sandboxDomain = domains.FirstOrDefault(static d => !d.IsCustomDomain);
        if (sandboxDomain is null)
        {
            return;
        }

        foreach (var domain in domains)
        {
            domain.SetPrimary(domain.Id == sandboxDomain.Id);
        }
    }
}
