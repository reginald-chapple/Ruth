using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Slices.Operators;

internal sealed class GetPendingGoLiveQueueQueryHandler(TenancyDbContext dbContext)
    : IQueryHandler<GetPendingGoLiveQueueQuery, IReadOnlyList<TenantSummaryResponse>>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Result<IReadOnlyList<TenantSummaryResponse>>> HandleAsync(
        GetPendingGoLiveQueueQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantsQuery = _dbContext.Tenants.AsNoTracking();
        if (query.LifecycleFilter.HasValue)
        {
            var stage = query.LifecycleFilter.Value;
            tenantsQuery = tenantsQuery.Where(x => x.LifecycleStage == stage);
        }

        var tenants = await tenantsQuery
            .OrderByDescending(x => x.SubmittedForReviewOnUtc)
            .ThenByDescending(x => x.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        var tenantIds = tenants.Select(static x => x.Id).ToArray();
        var primaryDomains = await _dbContext
            .TenantDomains.AsNoTracking()
            .Where(x => tenantIds.Contains(x.TargetTenantId) && x.IsPrimary)
            .ToDictionaryAsync(x => x.TargetTenantId, x => x.Hostname, cancellationToken);

        IReadOnlyList<TenantSummaryResponse> summaries =
        [
            .. tenants.Select(t => new TenantSummaryResponse(
                t.Id.Value,
                t.Slug,
                t.LegalName,
                t.Ein,
                t.IrsSubsectionCode,
                t.StateCode,
                t.LifecycleStage,
                t.IrsStatus,
                t.IrsOfficialName,
                t.StripeChargesEnabled,
                t.StripePayoutsEnabled,
                primaryDomains.GetValueOrDefault(t.Id, string.Empty),
                t.CreatedOnUtc,
                t.SubmittedForReviewOnUtc
            )),
        ];

        return Result.Success(summaries);
    }
}
