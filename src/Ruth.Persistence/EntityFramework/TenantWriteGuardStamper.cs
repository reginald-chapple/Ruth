using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
                    StampOrValidateAdded(entry, currentTenantId);
                    break;
                case EntityState.Modified:
                    ValidateTrackedEntry(entry, currentTenantId);
                    entry.Property(nameof(ITenantScoped.TenantId)).IsModified = false;
                    break;
                case EntityState.Deleted:
                    ValidateTrackedEntry(entry, currentTenantId);
                    break;
                case EntityState.Unchanged:
                    ValidateUnchangedAttachment(entry.Entity, currentTenantId);
                    break;
                case EntityState.Detached:
                default:
                    break;
            }
        }
    }

    private static void StampOrValidateAdded(
        EntityEntry<ITenantScoped> entry,
        TenantId currentTenantId
    )
    {
        var entity = entry.Entity;
        if (entity.TenantId.IsEmpty)
        {
            entity.AssignTenant(currentTenantId);
        }
        else if (entity.TenantId != currentTenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot insert '{entity.GetType().Name}' belonging to tenant '{entity.TenantId}' while active tenant is '{currentTenantId}'."
            );
        }

        var prop = entry.Property(nameof(ITenantScoped.TenantId));
        if (
            prop.CurrentValue is TenantId propTenant
            && !propTenant.IsEmpty
            && propTenant != currentTenantId
        )
        {
            throw new CrossTenantAccessException(
                $"Cannot insert '{entity.GetType().Name}' with property TenantId '{propTenant}' while active tenant is '{currentTenantId}'."
            );
        }

        prop.CurrentValue = currentTenantId;
    }

    private static void ValidateTrackedEntry(
        EntityEntry<ITenantScoped> entry,
        TenantId currentTenantId
    )
    {
        var entity = entry.Entity;
        var prop = entry.Property(nameof(ITenantScoped.TenantId));
        var originalTenant = prop.OriginalValue is TenantId orig ? orig : TenantId.Empty;
        var currentPropTenant = prop.CurrentValue is TenantId curr ? curr : TenantId.Empty;

        if (
            entity.TenantId != currentTenantId
            || originalTenant != currentTenantId
            || currentPropTenant != currentTenantId
        )
        {
            throw new CrossTenantAccessException(
                $"Cannot modify or delete '{entity.GetType().Name}' (original='{originalTenant}', current='{currentPropTenant}') while active tenant is '{currentTenantId}'."
            );
        }
    }

    private static void ValidateUnchangedAttachment(ITenantScoped entity, TenantId currentTenantId)
    {
        if (entity.TenantId != currentTenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot attach '{entity.GetType().Name}' belonging to tenant '{entity.TenantId}' while active tenant is '{currentTenantId}'."
            );
        }
    }
}
