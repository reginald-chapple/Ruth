using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Contracts.Verification;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal sealed class VerifyCustomDomainCommandHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    ITenantHostResolver hostResolver,
    IDnsTxtVerificationProvider dnsProvider,
    IClock clock
) : ICommandHandler<VerifyCustomDomainCommand, TenantDomainResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly ITenantHostResolver _hostResolver = hostResolver
        ?? throw new ArgumentNullException(nameof(hostResolver));
    private readonly IDnsTxtVerificationProvider _dnsProvider = dnsProvider
        ?? throw new ArgumentNullException(nameof(dnsProvider));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<TenantDomainResponse>> HandleAsync(
        VerifyCustomDomainCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var domainId = TenantDomainId.From(command.DomainId);
        var domain = await _dbContext.TenantDomains.FirstOrDefaultAsync(
            x => x.Id == domainId && x.TargetTenantId == _tenantContext.TenantId,
            cancellationToken
        );
        if (domain is null)
        {
            return TenancyErrors.DomainNotFound;
        }

        if (!domain.IsVerified)
        {
            var txtName = domain.VerificationTxtName ?? $"_org-verify.{domain.Hostname}";
            var resolvedTxtRecords = await _dnsProvider.ResolveTxtRecordsAsync(
                txtName,
                command.ObservedTxtToken,
                cancellationToken
            );
            var matchedToken = resolvedTxtRecords.FirstOrDefault(r =>
                string.Equals(r, domain.VerificationTxtValue, StringComparison.Ordinal)
            );
            var verifyResult = domain.VerifyOwnership(matchedToken, _clock.UtcNow);
            if (verifyResult.IsFailure)
            {
                return Result.Failure<TenantDomainResponse>(verifyResult.Errors);
            }
        }

        if (domain.MakePrimaryWhenVerified && _tenantContext.IsLive)
        {
            var others = await _dbContext
                .TenantDomains.Where(x =>
                    x.TargetTenantId == _tenantContext.TenantId && x.Id != domain.Id && x.IsPrimary
                )
                .ToListAsync(cancellationToken);
            foreach (var other in others)
            {
                other.SetPrimary(isPrimary: false);
            }

            domain.SetPrimary(isPrimary: true);
        }

        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        _hostResolver.InvalidateHost(domain.Hostname);
        return TenantComplianceMapper.MapDomain(domain);
    }
}
