using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Infrastructure;

internal sealed class CommunicationTenantSeedContributor(
    CommunicationDbContext dbContext,
    IClock clock
) : ITenantSeedContributor
{
    public int Order => 50;

    public Task SeedTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return BuiltInSystemCommunicationsSeeder.SeedAsync(
            dbContext,
            clock.UtcNow,
            cancellationToken
        );
    }
}
