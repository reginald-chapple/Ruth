using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal static class EventsJsonbListConventions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static void ConfigureStringList<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyList<string>>> propertyExpression,
        string columnName
    )
        where TEntity : class
    {
        var converter = new ValueConverter<IReadOnlyList<string>, string>(
            static list => JsonSerializer.Serialize(list, _jsonOptions),
            static json => DeserializeList(json)
        );

        var comparer = new ValueComparer<IReadOnlyList<string>>(
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

    private static List<string> DeserializeList(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? [];
    }

    private static List<string> CloneList(IReadOnlyList<string> source)
    {
        return [.. source];
    }

    private static bool ListsEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static int ComputeHash(IReadOnlyList<string> list)
    {
        var hash = new HashCode();
        foreach (var item in list)
        {
            hash.Add(item, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
