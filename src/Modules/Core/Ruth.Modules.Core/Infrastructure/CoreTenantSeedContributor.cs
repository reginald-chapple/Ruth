using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Metadata;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Core.Infrastructure;

internal sealed class CoreTenantSeedContributor(
    CoreDbContext dbContext,
    IEnumerable<EntityTypeRegistration> registrations,
    ICoreReadCache readCache
) : ITenantSeedContributor
{
    public int Order => 20;

    public async Task SeedTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default
    )
    {
        await SyncEntityTypesAsync(dbContext, registrations, tenantId, cancellationToken);
        await readCache.InvalidateAllAsync(cancellationToken);
    }

    internal static async Task SyncEntityTypesAsync(
        CoreDbContext dbContext,
        IEnumerable<EntityTypeRegistration> registrations,
        TenantId tenantId,
        CancellationToken cancellationToken
    )
    {
        var existingByKey = await dbContext.EntityTypeDescriptors.ToDictionaryAsync(
            static e => e.Key,
            StringComparer.OrdinalIgnoreCase,
            cancellationToken
        );

        foreach (var registration in registrations)
        {
            if (existingByKey.TryGetValue(registration.Key, out var existing))
            {
                existing.SyncFrom(registration);
                continue;
            }

            var created = EntityTypeDescriptor.FromRegistration(registration, tenantId);
            if (created.IsSuccess)
            {
                _ = dbContext.EntityTypeDescriptors.Add(created.Value);
                existingByKey[registration.Key] = created.Value;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
