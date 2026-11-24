using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.EntityFramework;

internal static class AuditAndConcurrencyStamper
{
    public static void StampAuditMetadata(DbContext context, DateTimeOffset utcNow)
    {
        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedOnUtc == default)
                {
                    entry.Entity.SetCreated(utcNow);
                }

                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditableEntity.CreatedOnUtc)).IsModified = false;
                entry.Entity.SetModified(utcNow);
            }
        }
    }

    public static void UpdateConcurrencyTokens(DbContext context, DateTimeOffset utcNow)
    {
        foreach (var entry in context.ChangeTracker.Entries<IHasConcurrencyToken>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.ConcurrencyStamp == Guid.Empty)
                {
                    var initialStamp = StronglyTypedId.NextGuid(utcNow);
                    SetConcurrencyStamp(entry, initialStamp);
                }

                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var nextStamp = StronglyTypedId.NextGuid(utcNow);
                SetConcurrencyStamp(entry, nextStamp);
            }
        }
    }

    private static void SetConcurrencyStamp(EntityEntry<IHasConcurrencyToken> entry, Guid stamp)
    {
        entry.Entity.SetConcurrencyStamp(stamp);
        entry.Property(nameof(IHasConcurrencyToken.ConcurrencyStamp)).CurrentValue = stamp;
    }
}
