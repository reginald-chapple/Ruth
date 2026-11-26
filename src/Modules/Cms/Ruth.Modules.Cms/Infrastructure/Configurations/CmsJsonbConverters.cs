using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal static class CmsJsonbConverters
{
    private static readonly JsonSerializerOptions _options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static void ConfigureJsonbList<TEntity, TItem>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyList<TItem>>> propertyExpression,
        string columnName
    )
        where TEntity : class
    {
        var converter = new ValueConverter<IReadOnlyList<TItem>, string>(
            static list => JsonSerializer.Serialize(list, _options),
            static json => DeserializeList<TItem>(json)
        );

        var comparer = new ValueComparer<IReadOnlyList<TItem>>(
            static (left, right) =>
                string.Equals(
                    JsonSerializer.Serialize(left, _options),
                    JsonSerializer.Serialize(right, _options),
                    StringComparison.Ordinal
                ),
            static list =>
                JsonSerializer.Serialize(list, _options).GetHashCode(StringComparison.Ordinal),
            static list => DeserializeList<TItem>(JsonSerializer.Serialize(list, _options))
        );

        var property = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .IsRequired();
        property.Metadata.SetValueComparer(comparer);
    }

    public static void ConfigureJsonbValue<TEntity, TValue>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TValue>> propertyExpression,
        string columnName,
        Func<TValue> defaultFactory
    )
        where TEntity : class
        where TValue : class
    {
        var converter = new ValueConverter<TValue, string>(
            static val => JsonSerializer.Serialize(val, _options),
            json => DeserializeObject(json, defaultFactory)
        );

        var comparer = new ValueComparer<TValue>(
            static (left, right) =>
                string.Equals(
                    JsonSerializer.Serialize(left, _options),
                    JsonSerializer.Serialize(right, _options),
                    StringComparison.Ordinal
                ),
            static val =>
                JsonSerializer.Serialize(val, _options).GetHashCode(StringComparison.Ordinal),
            val => DeserializeObject(JsonSerializer.Serialize(val, _options), defaultFactory)
        );

        var property = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .IsRequired();
        property.Metadata.SetValueComparer(comparer);
    }

    private static List<TItem> DeserializeList<TItem>(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<TItem>>(json, _options) ?? [];
    }

    private static TValue DeserializeObject<TValue>(string json, Func<TValue> defaultFactory)
        where TValue : class
    {
        return string.IsNullOrWhiteSpace(json)
            ? defaultFactory()
            : JsonSerializer.Deserialize<TValue>(json, _options) ?? defaultFactory();
    }
}
