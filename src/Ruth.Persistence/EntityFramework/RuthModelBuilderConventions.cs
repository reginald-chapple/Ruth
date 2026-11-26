using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.SharedKernel;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.EntityFramework;

internal static class RuthModelBuilderConventions
{
    public static void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder,
        Assembly? additionalIdAssembly
    )
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        _ = configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
        _ = configurationBuilder
            .Properties<DateTimeOffset?>()
            .HaveConversion<NullableUtcDateTimeOffsetConverter>();

        ApplyStronglyTypedIdsFromAssembly(configurationBuilder, SharedKernelAssembly.Instance);

        if (
            additionalIdAssembly is not null
            && additionalIdAssembly != SharedKernelAssembly.Instance
        )
        {
            ApplyStronglyTypedIdsFromAssembly(configurationBuilder, additionalIdAssembly);
            foreach (var referenced in additionalIdAssembly.GetReferencedAssemblies())
            {
                if (
                    referenced.Name is not null
                    && referenced.Name.StartsWith("Ruth.Modules.", StringComparison.Ordinal)
                    && referenced.Name.EndsWith(".Contracts", StringComparison.Ordinal)
                )
                {
                    ApplyStronglyTypedIdsFromAssembly(
                        configurationBuilder,
                        Assembly.Load(referenced)
                    );
                }
            }
        }
    }

    public static void ConfigureConcurrencyTokens(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IHasConcurrencyToken).IsAssignableFrom(entityType.ClrType))
            {
                _ = modelBuilder
                    .Entity(entityType.ClrType)
                    .Property(nameof(IHasConcurrencyToken.ConcurrencyStamp))
                    .IsConcurrencyToken();
            }
        }
    }

    public static void ConfigureTenantIsolation(
        ModelBuilder modelBuilder,
        ModuleDbContext dbContext,
        bool isTenantScopedSchema
    )
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(dbContext);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            if (!isTenantScopedSchema)
            {
                _ = modelBuilder.Entity(entityType.ClrType).Ignore(nameof(ITenantScoped.TenantId));
                continue;
            }

            ConfigureTenantScopedEntity(modelBuilder, dbContext, entityType);
        }
    }

    public static void ConfigurePostgresRowVersion<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        _ = builder.Property<uint>("RowVersion").IsRowVersion();
    }

    private static void ConfigureTenantScopedEntity(
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
        ScopeUniqueIndexesToTenant(entityType, tenantProperty);
        EnsureLeadingTenantIndex(entityType, tenantProperty);
        dbContext.ApplyTenantQueryFilterForClrType(modelBuilder, entityType.ClrType);
    }

    private static void ScopeUniqueIndexesToTenant(
        IMutableEntityType entityType,
        IMutableProperty tenantProperty
    )
    {
        foreach (var index in entityType.GetIndexes().ToArray())
        {
            if (!index.IsUnique || index.Properties.Contains(tenantProperty))
            {
                continue;
            }

            var existingProperties = index.Properties.ToArray();
            var filter = index.GetFilter();
            _ = entityType.RemoveIndex(index);

            IMutableProperty[] compositeProperties = [tenantProperty, .. existingProperties];
            var replacement = entityType.AddIndex(compositeProperties);
            replacement.IsUnique = true;
            if (filter is not null)
            {
                replacement.SetFilter(filter);
            }
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

    private static void ApplyStronglyTypedIdsFromAssembly(
        ModelConfigurationBuilder configurationBuilder,
        Assembly assembly
    )
    {
        foreach (var type in assembly.GetTypes())
        {
            if (IsStronglyTypedIdStruct(type))
            {
                var converterType = typeof(StronglyTypedIdValueConverter<>).MakeGenericType(type);
                _ = configurationBuilder.Properties(type).HaveConversion(converterType);
            }
        }
    }

    private static bool IsStronglyTypedIdStruct(Type type)
    {
        if (!type.IsValueType || type.IsEnum || type.IsGenericTypeDefinition)
        {
            return false;
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (
                iface.IsGenericType
                && iface.GetGenericTypeDefinition() == typeof(IStronglyTypedId<>)
                && iface.GetGenericArguments()[0] == type
            )
            {
                return true;
            }
        }

        return false;
    }
}
