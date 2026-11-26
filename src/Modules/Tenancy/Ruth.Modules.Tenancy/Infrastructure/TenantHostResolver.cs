using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.Modules.Tenancy.Domain;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Infrastructure;

internal sealed class TenantHostResolver(
    IServiceScopeFactory scopeFactory,
    IOptions<TenancyOptions> options,
    IClock clock
) : ITenantHostResolver
{
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    private readonly ConcurrentDictionary<string, CachedResolution> _cache = new(
        StringComparer.OrdinalIgnoreCase
    );

    private sealed record CachedResolution(ResolvedHostResponse Response, DateTimeOffset ExpiresAt);

    public async ValueTask<ResolvedHostResponse> ResolveHostAsync(
        string hostname,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedHost = TenantSlugPolicy.StripPort(hostname);
        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            return BuildUnknown(string.Empty);
        }

        if (IsOperatorHost(normalizedHost))
        {
            return new ResolvedHostResponse(
                ResolvedHostKind.PlatformOperator,
                TenantId.Empty,
                TenantSlug: null,
                OrganizationName: null,
                TenantLifecycleStage.Unresolved,
                normalizedHost
            );
        }

        if (
            _options.DedicatedWorkspaceTenantId is { } dedicatedId
            && dedicatedId != Guid.Empty
            && IsLoopbackHost(normalizedHost)
        )
        {
            return new ResolvedHostResponse(
                ResolvedHostKind.TenantWorkspace,
                TenantId.From(dedicatedId),
                "workspace",
                OrganizationName: null,
                TenantLifecycleStage.Live,
                normalizedHost
            );
        }

        if (IsSignupHost(normalizedHost))
        {
            return new ResolvedHostResponse(
                ResolvedHostKind.PlatformSignup,
                TenantId.Empty,
                TenantSlug: null,
                OrganizationName: null,
                TenantLifecycleStage.Unresolved,
                normalizedHost
            );
        }

        var nowUtc = _clock.UtcNow;
        if (_cache.TryGetValue(normalizedHost, out var cached) && nowUtc < cached.ExpiresAt)
        {
            return cached.Response;
        }

        var resolved = await QueryDatabaseForHostAsync(normalizedHost, cancellationToken);
        _cache[normalizedHost] = new CachedResolution(resolved, _clock.UtcNow + _cacheTtl);
        return resolved;
    }

    public void InvalidateHost(string? hostname = null)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            _cache.Clear();
            return;
        }

        var normalized = TenantSlugPolicy.StripPort(hostname);
        _ = _cache.TryRemove(normalized, out _);
    }

    private async Task<ResolvedHostResponse> QueryDatabaseForHostAsync(
        string normalizedHost,
        CancellationToken cancellationToken
    )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var match = await (
            from domain in dbContext.TenantDomains.AsNoTracking()
            join tenant in dbContext.Tenants.AsNoTracking()
                on domain.TargetTenantId equals tenant.Id
            where domain.Hostname == normalizedHost
            select new
            {
                domain.IsCustomDomain,
                domain.IsVerified,
                TenantId = tenant.Id,
                tenant.Slug,
                tenant.DisplayName,
                tenant.LifecycleStage,
            }
        ).FirstOrDefaultAsync(cancellationToken);

        var isIneligible =
            match is null
            || !match.IsVerified
            || match.LifecycleStage == TenantLifecycleStage.Suspended
            || (match.IsCustomDomain && match.LifecycleStage != TenantLifecycleStage.Live);

        return isIneligible
            ? BuildUnknown(normalizedHost)
            : new ResolvedHostResponse(
                ResolvedHostKind.TenantWorkspace,
                match!.TenantId,
                match.Slug,
                match.DisplayName,
                match.LifecycleStage,
                normalizedHost
            );
    }

    private bool IsOperatorHost(string normalizedHost)
    {
        return string.Equals(
            normalizedHost,
            TenantSlugPolicy.StripPort(_options.PlatformOperatorHost),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private bool IsSignupHost(string normalizedHost)
    {
        foreach (var candidate in _options.PlatformSignupHosts)
        {
            if (
                string.Equals(
                    normalizedHost,
                    TenantSlugPolicy.StripPort(candidate),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLoopbackHost(string normalizedHost)
    {
        return string.Equals(normalizedHost, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedHost, "127.0.0.1", StringComparison.Ordinal);
    }

    private static ResolvedHostResponse BuildUnknown(string normalizedHost)
    {
        return new ResolvedHostResponse(
            ResolvedHostKind.Unknown,
            TenantId.Empty,
            TenantSlug: null,
            OrganizationName: null,
            TenantLifecycleStage.Unresolved,
            normalizedHost
        );
    }
}
