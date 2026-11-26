using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Infrastructure.Onboarding;

internal sealed class OnboardingStateReader(IServiceScopeFactory scopeFactory)
    : IOnboardingStateReader
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ConcurrentDictionary<TenantId, OnboardingStatusResponse> _completedByTenant =
    [];

    public async ValueTask<OnboardingStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            return new OnboardingStatusResponse(
                IsOnboardingRequired: true,
                IsSelfRegistrationLocked: false,
                OrganizationName: null
            );
        }

        if (_completedByTenant.TryGetValue(tenantId, out var cached))
        {
            return cached;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var adminRoleId = SecurityRole.GetAdministratorsRoleId(tenantId);

        var organization = await dbContext
            .OrganizationProfiles.AsNoTracking()
            .OrderBy(static o => o.SingletonKey)
            .Select(static o => new
            {
                o.Name,
                o.IsOnboardingCompleted,
                o.SelfRegistrationLocked,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var adminExists = await dbContext
            .UserRoles.AsNoTracking()
            .AnyAsync(ur => ur.SecurityRoleId == adminRoleId, cancellationToken);

        if (
            organization is { IsOnboardingCompleted: true, SelfRegistrationLocked: true }
            && adminExists
        )
        {
            var completed = new OnboardingStatusResponse(
                IsOnboardingRequired: false,
                IsSelfRegistrationLocked: true,
                OrganizationName: organization.Name
            );
            _completedByTenant[tenantId] = completed;
            return completed;
        }

        return new OnboardingStatusResponse(
            IsOnboardingRequired: true,
            IsSelfRegistrationLocked: organization?.SelfRegistrationLocked ?? false,
            OrganizationName: organization?.Name
        );
    }

    public void InvalidateCache()
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            _completedByTenant.Clear();
            return;
        }

        _ = _completedByTenant.TryRemove(tenantId, out _);
    }
}
