using System.Reflection;
using Microsoft.EntityFrameworkCore;
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

        TenantModelIsolationHelper.PruneUnreachableImplicitTypes(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            if (entityType.IsOwned())
            {
                TenantModelIsolationHelper.ValidateOwnedEntityTablePlacement(
                    dbContext,
                    entityType,
                    isTenantScopedSchema
                );
                continue;
            }

            if (entityType.BaseType is not null)
            {
                TenantModelIsolationHelper.ConfigureDerivedEntityIsolation(
                    entityType,
                    isTenantScopedSchema
                );
                continue;
            }

            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                if (isTenantScopedSchema)
                {
                    throw new InvalidOperationException(
                        $"Entity '{entityType.ClrType.FullName}' in tenant-scoped '{dbContext.GetType().Name}' must implement '{nameof(ITenantScoped)}'."
                    );
                }

                continue;
            }

            if (!isTenantScopedSchema)
            {
                _ = modelBuilder.Entity(entityType.ClrType).Ignore(nameof(ITenantScoped.TenantId));
                continue;
            }

            TenantModelIsolationHelper.ConfigureTenantScopedEntity(
                modelBuilder,
                dbContext,
                entityType
            );
        }
    }

    public static void ConfigurePostgresRowVersion<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        _ = builder.Property<uint>("RowVersion").IsRowVersion();
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
