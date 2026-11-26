using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal static class WorkflowJsonbConventions
{
    private static readonly JsonSerializerOptions _jsonOptions = CreateOptions();

    public static void ConfigureList<TEntity, TItem>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyList<TItem>>> propertyExpression,
        string columnName
    )
        where TEntity : class
    {
        var converter = new ValueConverter<IReadOnlyList<TItem>, string>(
            static list => JsonSerializer.Serialize(list, _jsonOptions),
            static json => DeserializeList<TItem>(json)
        );

        var comparer = new ValueComparer<IReadOnlyList<TItem>>(
            static (left, right) => ListsEqual(left, right),
            static list => ComputeHash(list),
            static list => CloneList(list)
        );

        var prop = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .UsePropertyAccessMode(PropertyAccessMode.Property)
            .IsRequired();
        prop.Metadata.SetValueComparer(comparer);
    }

    public static void ConfigureNullableObject<TEntity, TObject>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TObject?>> propertyExpression,
        string columnName
    )
        where TEntity : class
        where TObject : class
    {
        var converter = new ValueConverter<TObject?, string?>(
            static obj => obj == null ? null : JsonSerializer.Serialize(obj, _jsonOptions),
            static json =>
                string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<TObject>(json, _jsonOptions)
        );

        var comparer = new ValueComparer<TObject?>(
            static (left, right) => JsonEquals(left, right),
            static obj =>
                obj == null
                    ? 0
                    : StringComparer.Ordinal.GetHashCode(
                        JsonSerializer.Serialize(obj, _jsonOptions)
                    ),
            static obj =>
                obj == null
                    ? null
                    : JsonSerializer.Deserialize<TObject>(
                        JsonSerializer.Serialize(obj, _jsonOptions),
                        _jsonOptions
                    )
        );

        var prop = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter);
        prop.Metadata.SetValueComparer(comparer);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
    }

    private static bool JsonEquals<TObject>(TObject? left, TObject? right)
        where TObject : class
    {
        return ReferenceEquals(left, right)
            || (
                left is not null
                && right is not null
                && string.Equals(
                    JsonSerializer.Serialize(left, _jsonOptions),
                    JsonSerializer.Serialize(right, _jsonOptions),
                    StringComparison.Ordinal
                )
            );
    }

    private static List<TItem> DeserializeList<TItem>(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<TItem>>(json, _jsonOptions) ?? [];
    }

    private static List<TItem> CloneList<TItem>(IReadOnlyList<TItem> source)
    {
        return [.. source];
    }

    private static bool ListsEqual<TItem>(IReadOnlyList<TItem>? left, IReadOnlyList<TItem>? right)
    {
        return ReferenceEquals(left, right)
            || (left is not null && right is not null && left.SequenceEqual(right));
    }

    private static int ComputeHash<TItem>(IReadOnlyList<TItem> list)
    {
        var hash = new HashCode();
        foreach (var item in list)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}
