using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Slices.Signup;

internal sealed class SignupTaxExemptTenantCommandHandler(
    TenancyDbContext dbContext,
    IServiceScopeFactory scopeFactory,
    ITenantHostResolver hostResolver,
    ActiveTenantRegistry activeTenantRegistry,
    IOptions<TenancyOptions> options,
    IClock clock
) : ICommandHandler<SignupTaxExemptTenantCommand, SignupTaxExemptTenantResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ITenantHostResolver _hostResolver = hostResolver
        ?? throw new ArgumentNullException(nameof(hostResolver));
    private readonly ActiveTenantRegistry _activeTenantRegistry = activeTenantRegistry
        ?? throw new ArgumentNullException(nameof(activeTenantRegistry));
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<SignupTaxExemptTenantResponse>> HandleAsync(
        SignupTaxExemptTenantCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = _clock.UtcNow;
        var tenantResult = Tenant.RegisterSandbox(
            command.LegalName,
            command.DisplayName,
            command.Slug,
            command.Ein,
            command.IrsSubsectionCode,
            command.StateCode,
            command.TimeZoneId,
            nowUtc
        );
        if (tenantResult.IsFailure)
        {
            return Result.Failure<SignupTaxExemptTenantResponse>(tenantResult.Errors);
        }

        var tenant = tenantResult.Value;
        var uniquenessCheck = await CheckUniquenessAsync(tenant, cancellationToken);
        if (uniquenessCheck.IsFailure)
        {
            return Result.Failure<SignupTaxExemptTenantResponse>(uniquenessCheck.Errors);
        }

        var sandboxDomain = TenantDomain.CreateSandboxDomain(
            tenant.Id,
            tenant.Slug,
            _options.SandboxBaseDomain,
            nowUtc
        );
        var persisted = await TryPersistTenantAsync(tenant, sandboxDomain, cancellationToken);
        if (persisted.IsFailure)
        {
            return Result.Failure<SignupTaxExemptTenantResponse>(persisted.Errors);
        }

        var bootstrapResult = await TenantWorkspaceBootstrapper.BootstrapAsync(
            _scopeFactory,
            tenant,
            command,
            cancellationToken
        );
        if (bootstrapResult.IsFailure)
        {
            await RollbackProvisionedTenantAsync(tenant.Id, sandboxDomain.Hostname);
            return Result.Failure<SignupTaxExemptTenantResponse>(bootstrapResult.Errors);
        }

        return new SignupTaxExemptTenantResponse(
            tenant.Id.Value,
            tenant.Slug,
            sandboxDomain.Hostname,
            bootstrapResult.Value,
            tenant.LifecycleStage,
            tenant.CreatedOnUtc
        );
    }

    private async Task<Result> TryPersistTenantAsync(
        Tenant tenant,
        TenantDomain sandboxDomain,
        CancellationToken cancellationToken
    )
    {
        _ = _dbContext.Tenants.Add(tenant);
        _ = _dbContext.TenantDomains.Add(sandboxDomain);

        try
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (TryMapUniqueViolation(ex, out var conflictError))
        {
            _dbContext.ChangeTracker.Clear();
            return conflictError;
        }

        _activeTenantRegistry.Invalidate();
        _hostResolver.InvalidateHost(sandboxDomain.Hostname);
        return Result.Success();
    }

    private async Task<Result> CheckUniquenessAsync(
        Tenant candidate,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext
                .Tenants.AsNoTracking()
                .AnyAsync(x => x.Slug == candidate.Slug, cancellationToken)
                ? TenancyErrors.SlugAlreadyTaken
            : await _dbContext
                .Tenants.AsNoTracking()
                .AnyAsync(x => x.Ein == candidate.Ein, cancellationToken)
                ? TenancyErrors.EinAlreadyRegistered
            : Result.Success();
    }

    private async Task RollbackProvisionedTenantAsync(TenantId tenantId, string hostname)
    {
        await using var cleanupScope = _scopeFactory.CreateAsyncScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        _ = await cleanupDb
            .Tenants.Where(x => x.Id == tenantId)
            .ExecuteDeleteAsync(CancellationToken.None);
        _activeTenantRegistry.Invalidate();
        _hostResolver.InvalidateHost(hostname);
    }

    private static bool TryMapUniqueViolation(DbUpdateException exception, out DomainError error)
    {
        if (
            exception.InnerException is PostgresException pg
            && string.Equals(
                pg.SqlState,
                PostgresErrorCodes.UniqueViolation,
                StringComparison.Ordinal
            )
        )
        {
            error =
                pg.ConstraintName?.Contains("ein", StringComparison.OrdinalIgnoreCase) == true
                    ? TenancyErrors.EinAlreadyRegistered
                    : TenancyErrors.SlugAlreadyTaken;
            return true;
        }

        error = default!;
        return false;
    }
}
