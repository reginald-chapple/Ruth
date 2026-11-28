using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Ruth.Persistence.EntityFramework;
using Ruth.Persistence.Outbox;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.ArchitectureTests;

internal static class TenantDbContextModelInspector
{
    public static void AssertTenantDbContextModel(ModuleDbContext context, bool isTenantScoped)
    {
        var contextName = context.GetType().Name;
        Assert.True(isTenantScoped, $"{contextName} must have IsTenantScopedSchema = true.");

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                var ownerTable = entityType.FindOwnership()?.PrincipalEntityType.GetTableName();
                var ownedTable = entityType.GetTableName();
                Assert.Equal(ownerTable, ownedTable);
                continue;
            }

            if (entityType.BaseType is not null)
            {
                Assert.Equal(entityType.GetRootType().GetTableName(), entityType.GetTableName());
                continue;
            }

            Assert.True(
                typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType),
                $"{contextName} entity '{entityType.ClrType.FullName}' must implement ITenantScoped."
            );

            var tenantProp = entityType.FindProperty(nameof(ITenantScoped.TenantId));
            Assert.NotNull(tenantProp);
            Assert.False(tenantProp.IsNullable);
            Assert.Equal("tenant_id", tenantProp.GetColumnName());
            Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
        }
    }

    public static void AssertTenantDbContextIndexesAndOutbox(ModuleDbContext context)
    {
        var contextName = context.GetType().Name;
        Assert.NotNull(context.Model.FindEntityType(typeof(OutboxMessage)));
        Assert.NotNull(context.Model.FindEntityType(typeof(InboxMessage)));

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                continue;
            }

            var rootType = entityType.GetRootType();
            var tenantProp = rootType.FindProperty(nameof(ITenantScoped.TenantId));
            Assert.NotNull(tenantProp);

            foreach (var index in entityType.GetDeclaredIndexes())
            {
                if (index.IsUnique)
                {
                    Assert.True(
                        index.Properties.Contains(tenantProp),
                        $"{contextName}.{entityType.ClrType.Name} unique index ({string.Join(',', index.Properties.Select(static p => p.Name))}) must include TenantId."
                    );
                }
            }

            if (entityType.BaseType is null)
            {
                AssertHasLeadingTenantKeyOrIndex(contextName, entityType, tenantProp);
            }
        }
    }

    private static void AssertHasLeadingTenantKeyOrIndex(
        string contextName,
        IEntityType entityType,
        IProperty tenantProp
    )
    {
        var pkLeadsWithTenant = entityType.FindPrimaryKey()?.Properties[0] == tenantProp;
        var indexLeadsWithTenant = entityType
            .GetIndexes()
            .Any(i => i.Properties.Count > 0 && i.Properties[0] == tenantProp);

        Assert.True(
            pkLeadsWithTenant || indexLeadsWithTenant,
            $"{contextName}.{entityType.ClrType.Name} must have a leading TenantId PK or index."
        );
    }
}
