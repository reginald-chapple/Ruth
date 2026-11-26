using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal sealed class StartStripeConnectOnboardingCommandHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    IStripeConnectProvider stripeConnectProvider
) : ICommandHandler<StartStripeConnectOnboardingCommand, StripeConnectOnboardingResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IStripeConnectProvider _stripeConnectProvider = stripeConnectProvider
        ?? throw new ArgumentNullException(nameof(stripeConnectProvider));

    public async Task<Result<StripeConnectOnboardingResponse>> HandleAsync(
        StartStripeConnectOnboardingCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (
            string.IsNullOrWhiteSpace(command.ReturnUrl)
            || string.IsNullOrWhiteSpace(command.RefreshUrl)
        )
        {
            return DomainError.Validation(
                "Tenancy.InvalidOnboardingUrls",
                "Stripe Connect return and refresh URLs are required."
            );
        }

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(
            x => x.Id == _tenantContext.TenantId,
            cancellationToken
        );
        if (tenant is null)
        {
            return TenancyErrors.TenantNotFound;
        }

        var session = await _stripeConnectProvider.CreateOrResumeOnboardingAsync(
            tenant.Id,
            tenant.StripeConnectAccountId,
            tenant.LegalName,
            tenant.Ein,
            command.ReturnUrl,
            command.RefreshUrl,
            cancellationToken
        );
        var accountStatus = await _stripeConnectProvider.GetAccountStatusAsync(
            session.AccountId,
            cancellationToken
        );

        tenant.AttachStripeConnectAccount(
            session.AccountId,
            accountStatus.ChargesEnabled,
            accountStatus.PayoutsEnabled
        );
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return new StripeConnectOnboardingResponse(
            session.AccountId,
            session.OnboardingUrl,
            _stripeConnectProvider.IsMockMode,
            session.ExpiresAtUtc
        );
    }
}
