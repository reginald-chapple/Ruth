using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Infrastructure;

/// <summary>
/// Resolves canonical HTTPS URLs for tenant workspaces strictly from verified platform control-plane domain records,
/// preventing HTTP Host / X-Forwarded-Host poisoning on password-reset, account-confirmation, preference, and RSVP links.
/// </summary>
internal sealed class CanonicalTenantUrlProvider(
    IServiceScopeFactory scopeFactory,
    IOptions<TenancyOptions> options
) : ICanonicalTenantUrlProvider
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    public async ValueTask<string> GetCanonicalBaseUrlAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var effectiveTenantId = !tenantId.IsEmpty ? tenantId : AmbientTenantContext.CurrentTenantId;
        if (!effectiveTenantId.IsEmpty)
        {
            var resolvedHost = await QueryVerifiedHostnameAsync(
                effectiveTenantId,
                cancellationToken
            );
            if (!string.IsNullOrWhiteSpace(resolvedHost))
            {
                return $"https://{resolvedHost}";
            }
        }

        var ambientSlug = AmbientTenantContext.CurrentState.TenantSlug;
        return !string.IsNullOrWhiteSpace(ambientSlug)
            ? $"https://{ambientSlug.Trim().ToLowerInvariant()}.{_options.SandboxBaseDomain.Trim().ToLowerInvariant()}"
            : string.Empty;
    }

    public async ValueTask<string> BuildTenantUrlAsync(
        TenantId tenantId,
        string relativePathAndQuery,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePathAndQuery);

        var trimmed = relativePathAndQuery.Trim();
        var normalizedPath = trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
        var baseUrl = await GetCanonicalBaseUrlAsync(tenantId, cancellationToken);
        return string.IsNullOrEmpty(baseUrl) ? normalizedPath : $"{baseUrl}{normalizedPath}";
    }

    private async Task<string?> QueryVerifiedHostnameAsync(
        TenantId tenantId,
        CancellationToken cancellationToken
    )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var tenant = await dbContext
            .Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(static t => new { t.Slug, t.LifecycleStage })
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant is null || tenant.LifecycleStage == TenantLifecycleStage.Suspended)
        {
            return null;
        }

        var domains = await dbContext
            .TenantDomains.AsNoTracking()
            .Where(d => d.TargetTenantId == tenantId && d.IsVerified)
            .Select(static d => new
            {
                d.Hostname,
                d.IsCustomDomain,
                d.IsPrimary,
            })
            .ToListAsync(cancellationToken);

        if (tenant.LifecycleStage == TenantLifecycleStage.Live)
        {
            var primaryCustom = domains.Find(static d => d.IsCustomDomain && d.IsPrimary);
            if (primaryCustom is not null)
            {
                return primaryCustom.Hostname;
            }
        }

        var sandboxDomain = domains.Find(static d => !d.IsCustomDomain);
        return sandboxDomain?.Hostname
            ?? $"{tenant.Slug}.{_options.SandboxBaseDomain}".ToLowerInvariant();
    }
}
