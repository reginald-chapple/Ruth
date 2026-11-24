using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Onboarding;

internal sealed class OnboardingStateReader(IServiceScopeFactory scopeFactory)
    : IOnboardingStateReader
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private OnboardingStatusResponse? _completedSnapshot;

    public async ValueTask<OnboardingStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default
    )
    {
        var cached = Volatile.Read(ref _completedSnapshot);
        if (cached is not null)
        {
            return cached;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

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
            .AnyAsync(
                static ur => ur.SecurityRoleId == SecurityRole.AdministratorsRoleId,
                cancellationToken
            );

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
            Volatile.Write(ref _completedSnapshot, completed);
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
        Volatile.Write(ref _completedSnapshot, null);
    }
}
