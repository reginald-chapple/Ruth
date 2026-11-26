using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal static class EngagementJsonbConventions
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

    private static JsonSerializerOptions CreateOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
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
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        var eq = EqualityComparer<TItem>.Default;
        for (var i = 0; i < left.Count; i++)
        {
            if (!eq.Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
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
