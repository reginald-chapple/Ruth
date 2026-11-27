using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.EntityFramework;

internal static class TenantModelIsolationHelper
{
    public static void PruneUnreachableImplicitTypes(ModelBuilder modelBuilder)
    {
        var allTypes = modelBuilder.Model.GetEntityTypes().ToArray();
        var reachable = new HashSet<IMutableEntityType>();
        var queue = new Queue<IMutableEntityType>();

        foreach (var entityType in allTypes)
        {
            if (
                ((IConventionEntityType)entityType).GetConfigurationSource()
                != ConfigurationSource.Convention
            )
            {
                _ = reachable.Add(entityType);
                queue.Enqueue(entityType);
            }
        }

        while (queue.Count > 0)
        {
            EnqueueConnectedEntityTypes(queue.Dequeue(), reachable, queue);
        }

        foreach (var entityType in allTypes)
        {
            if (!reachable.Contains(entityType))
            {
                _ = modelBuilder.Ignore(entityType.ClrType);
            }
        }
    }

    public static void ValidateOwnedEntityTablePlacement(
        ModuleDbContext dbContext,
        IMutableEntityType ownedType,
        bool isTenantScopedSchema
    )
    {
        if (!isTenantScopedSchema)
        {
            return;
        }

        var ownerType = ownedType.FindOwnership()?.PrincipalEntityType;
        if (ownerType is null)
        {
            return;
        }

        var ownerTable = ownerType.GetTableName();
        var ownedTable = ownedType.GetTableName();
        if (
            !string.IsNullOrEmpty(ownedTable)
            && !string.Equals(ownerTable, ownedTable, StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                $"Owned entity '{ownedType.DisplayName()}' in '{dbContext.GetType().Name}' maps to separate table '{ownedTable}' without '{nameof(ITenantScoped)}'."
            );
        }
    }

    public static void ConfigureDerivedEntityIsolation(
        IMutableEntityType derivedType,
        bool isTenantScopedSchema
    )
    {
        if (!isTenantScopedSchema)
        {
            return;
        }

        var rootType = derivedType.GetRootType();
        if (
            !string.Equals(
                derivedType.GetTableName(),
                rootType.GetTableName(),
                StringComparison.Ordinal
            )
        )
        {
            throw new InvalidOperationException(
                $"Derived entity '{derivedType.ClrType.FullName}' uses TPT table '{derivedType.GetTableName()}', which is unsupported in tenant RLS schemas."
            );
        }

        var tenantProperty = rootType.FindProperty(nameof(ITenantScoped.TenantId));
        if (tenantProperty is not null)
        {
            ScopeUniqueIndexesToTenant(
                derivedType,
                tenantProperty,
                [.. derivedType.GetDeclaredIndexes()]
            );
        }
    }

    public static void ConfigureTenantScopedEntity(
        ModelBuilder modelBuilder,
        ModuleDbContext dbContext,
        IMutableEntityType entityType
    )
    {
        _ = modelBuilder
            .Entity(entityType.ClrType)
            .Property(nameof(ITenantScoped.TenantId))
            .HasColumnName("tenant_id")
            .IsRequired();

        var tenantProperty = entityType.FindProperty(nameof(ITenantScoped.TenantId))!;
        ScopeUniqueIndexesToTenant(entityType, tenantProperty, [.. entityType.GetIndexes()]);
        EnsureLeadingTenantIndex(entityType, tenantProperty);
        dbContext.ApplyTenantQueryFilterForClrType(modelBuilder, entityType.ClrType);
    }

    private static void EnqueueConnectedEntityTypes(
        IMutableEntityType entityType,
        HashSet<IMutableEntityType> reachable,
        Queue<IMutableEntityType> queue
    )
    {
        TryVisit(entityType.BaseType, reachable, queue);
        foreach (var derived in entityType.GetDirectlyDerivedTypes())
        {
            TryVisit(derived, reachable, queue);
        }

        foreach (var nav in entityType.GetDeclaredNavigations())
        {
            TryVisit(nav.TargetEntityType, reachable, queue);
        }

        foreach (var skip in entityType.GetDeclaredSkipNavigations())
        {
            TryVisit(skip.TargetEntityType, reachable, queue);
            TryVisit(skip.JoinEntityType, reachable, queue);
        }

        foreach (var fk in entityType.GetDeclaredForeignKeys())
        {
            TryVisit(fk.PrincipalEntityType, reachable, queue);
        }

        foreach (var refFk in entityType.GetDeclaredReferencingForeignKeys())
        {
            TryVisit(refFk.DeclaringEntityType, reachable, queue);
        }
    }

    private static void TryVisit(
        IMutableEntityType? candidate,
        HashSet<IMutableEntityType> reachable,
        Queue<IMutableEntityType> queue
    )
    {
        if (candidate is not null && reachable.Add(candidate))
        {
            queue.Enqueue(candidate);
        }
    }

    private static void ScopeUniqueIndexesToTenant(
        IMutableEntityType entityType,
        IMutableProperty tenantProperty,
        IMutableIndex[] indexes
    )
    {
        foreach (var index in indexes)
        {
            if (!index.IsUnique || index.Properties.Contains(tenantProperty))
            {
                continue;
            }

            ReplaceWithTenantScopedUniqueIndex(entityType, tenantProperty, index);
        }
    }

    private static void ReplaceWithTenantScopedUniqueIndex(
        IMutableEntityType entityType,
        IMutableProperty tenantProperty,
        IMutableIndex index
    )
    {
        var existingProperties = index.Properties.ToArray();
        var logicalName = index.Name;
        var filter = index.GetFilter();
        var descending = index.IsDescending?.ToArray();
        var included = index.GetIncludeProperties()?.ToArray();
        var explicitDbName = GetExplicitDatabaseIndexName(index);
        var annotations = CaptureCustomIndexAnnotations(index);

        _ = entityType.RemoveIndex(index);

        IMutableProperty[] compositeProperties = [tenantProperty, .. existingProperties];
        var replacement = logicalName is not null
            ? entityType.AddIndex(compositeProperties, logicalName)
            : entityType.AddIndex(compositeProperties);
        replacement.IsUnique = true;
        CopyCapturedIndexMetadata(
            replacement,
            filter,
            descending,
            included,
            explicitDbName,
            annotations
        );
    }

    private static string? GetExplicitDatabaseIndexName(IMutableIndex index)
    {
        return
            ((IConventionIndex)index).GetDatabaseNameConfigurationSource()
            == ConfigurationSource.Explicit
            ? index.GetDatabaseName()
            : null;
    }

    private static IAnnotation[] CaptureCustomIndexAnnotations(IMutableIndex index)
    {
        return
        [
            .. index
                .GetAnnotations()
                .Where(static a =>
                    !string.Equals(a.Name, RelationalAnnotationNames.Name, StringComparison.Ordinal)
                    && !string.Equals(
                        a.Name,
                        RelationalAnnotationNames.Filter,
                        StringComparison.Ordinal
                    )
                ),
        ];
    }

    private static void CopyCapturedIndexMetadata(
        IMutableIndex replacement,
        string? filter,
        bool[]? descending,
        string[]? included,
        string? explicitDbName,
        IAnnotation[] annotations
    )
    {
        if (filter is not null)
        {
            replacement.SetFilter(filter);
        }

        if (descending is { Length: > 0 })
        {
            replacement.IsDescending = [false, .. descending];
        }

        if (included is { Length: > 0 })
        {
            replacement.SetIncludeProperties(included);
        }

        if (explicitDbName is not null)
        {
            replacement.SetDatabaseName(explicitDbName);
        }

        foreach (var annotation in annotations)
        {
            replacement.SetAnnotation(annotation.Name, annotation.Value);
        }
    }

    private static void EnsureLeadingTenantIndex(
        IMutableEntityType entityType,
        IMutableProperty tenantProperty
    )
    {
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey is not null && primaryKey.Properties[0] == tenantProperty)
        {
            return;
        }

        foreach (var index in entityType.GetIndexes())
        {
            if (index.Properties.Count > 0 && index.Properties[0] == tenantProperty)
            {
                return;
            }
        }

        _ = entityType.AddIndex([tenantProperty]);
    }
}
