using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal sealed class GetCurrentTenantComplianceQueryHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    IStripeConnectProvider stripeConnectProvider
) : IQueryHandler<GetCurrentTenantComplianceQuery, TenantComplianceStatusResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IStripeConnectProvider _stripeConnectProvider = stripeConnectProvider
        ?? throw new ArgumentNullException(nameof(stripeConnectProvider));

    public async Task<Result<TenantComplianceStatusResponse>> HandleAsync(
        GetCurrentTenantComplianceQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var status = await TenantComplianceMapper.LoadStatusAsync(
            _dbContext,
            _tenantContext.TenantId,
            _stripeConnectProvider.IsMockMode,
            cancellationToken
        );
        return status is not null ? status : TenancyErrors.TenantNotFound;
    }
}
