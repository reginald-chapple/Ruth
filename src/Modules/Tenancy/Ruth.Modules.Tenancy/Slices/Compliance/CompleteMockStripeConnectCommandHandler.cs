using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal sealed class CompleteMockStripeConnectCommandHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    IStripeConnectProvider stripeConnectProvider
) : ICommandHandler<CompleteMockStripeConnectCommand, TenantComplianceStatusResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IStripeConnectProvider _stripeConnectProvider = stripeConnectProvider
        ?? throw new ArgumentNullException(nameof(stripeConnectProvider));

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

        var accountId = tenant.StripeConnectAccountId;
        if (string.IsNullOrWhiteSpace(accountId))
        {
            var session = await _stripeConnectProvider.CreateOrResumeOnboardingAsync(
                tenant.Id,
                existingAccountId: null,
                tenant.LegalName,
                tenant.Ein,
                "https://localhost/admin/compliance",
                "https://localhost/admin/compliance",
                cancellationToken
            );
            accountId = session.AccountId;
        }

        var completed = await _stripeConnectProvider.CompleteMockOnboardingAsync(
            accountId,
            cancellationToken
        );
        tenant.AttachStripeConnectAccount(
            completed.AccountId,
            completed.ChargesEnabled,
            completed.PayoutsEnabled
        );
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        var status = await TenantComplianceMapper.LoadStatusAsync(
            _dbContext,
            tenant.Id,
            _stripeConnectProvider.IsMockMode,
            cancellationToken
        );
        return status is not null ? status : TenancyErrors.TenantNotFound;
    }
}
