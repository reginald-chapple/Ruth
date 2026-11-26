using Microsoft.EntityFrameworkCore;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.EntityFramework;

internal static class TenantWriteGuardStamper
{
    public static void EnforceAndStampTenant(ModuleDbContext context)
    {
        if (!context.RequiresTenantIsolation)
        {
            return;
        }

        var currentTenantId = context.CurrentTenantId;
        if (currentTenantId.IsEmpty)
        {
            throw new TenantContextRequiredException(
                $"Cannot save changes on '{context.GetType().Name}' without an active tenant context."
            );
        }

        foreach (var entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    StampOrValidateAdded(entry.Entity, currentTenantId);
                    entry.Property(nameof(ITenantScoped.TenantId)).CurrentValue = currentTenantId;
                    break;
                case EntityState.Modified:
                    ValidateExistingTenant(entry.Entity, currentTenantId);
                    entry.Property(nameof(ITenantScoped.TenantId)).IsModified = false;
                    break;
                case EntityState.Deleted:
                    ValidateExistingTenant(entry.Entity, currentTenantId);
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }
        }
    }

    private static void StampOrValidateAdded(ITenantScoped entity, TenantId currentTenantId)
    {
        if (entity.TenantId.IsEmpty)
        {
            entity.AssignTenant(currentTenantId);
            return;
        }

        if (entity.TenantId != currentTenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot insert '{entity.GetType().Name}' belonging to tenant '{entity.TenantId}' while active tenant is '{currentTenantId}'."
            );
        }
    }

    private static void ValidateExistingTenant(ITenantScoped entity, TenantId currentTenantId)
    {
        if (entity.TenantId != currentTenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot modify or delete '{entity.GetType().Name}' belonging to tenant '{entity.TenantId}' while active tenant is '{currentTenantId}'."
            );
        }
    }
}
