using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.SharedKernel.Primitives;

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

        if (additionalIdAssembly is not null)
        {
            ApplyStronglyTypedIdsFromAssembly(configurationBuilder, additionalIdAssembly);
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
