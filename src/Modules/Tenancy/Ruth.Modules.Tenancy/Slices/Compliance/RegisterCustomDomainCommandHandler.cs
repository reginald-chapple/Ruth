using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal sealed class RegisterCustomDomainCommandHandler(
    TenancyDbContext dbContext,
    ITenantContext tenantContext,
    IOptions<TenancyOptions> options,
    IClock clock
) : ICommandHandler<RegisterCustomDomainCommand, TenantDomainResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<TenantDomainResponse>> HandleAsync(
        RegisterCustomDomainCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var domainResult = TenantDomain.CreateCustomDomain(
            _tenantContext.TenantId,
            command.Hostname,
            _options.SandboxBaseDomain,
            command.MakePrimaryWhenVerified,
            _clock.UtcNow
        );
        if (domainResult.IsFailure)
        {
            return Result.Failure<TenantDomainResponse>(domainResult.Errors);
        }

        var candidate = domainResult.Value;
        var exists = await _dbContext
            .TenantDomains.AsNoTracking()
            .AnyAsync(x => x.Hostname == candidate.Hostname, cancellationToken);
        if (exists)
        {
            return TenancyErrors.DomainAlreadyRegistered;
        }

        _ = _dbContext.TenantDomains.Add(candidate);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        return TenantComplianceMapper.MapDomain(candidate);
    }
}
