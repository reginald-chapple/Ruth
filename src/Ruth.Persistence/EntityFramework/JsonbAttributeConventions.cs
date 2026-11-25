using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Persistence.EntityFramework;

/// <summary>EF Core conventions for PostgreSQL JSONB attribute columns with GIN indexes.</summary>
public static class JsonbAttributeConventions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Maps an <see cref="IReadOnlyDictionary{TKey,TValue}" /> property to a PostgreSQL JSONB column
    /// with a structural <see cref="ValueComparer{T}" /> and an optional GIN index.
    /// </summary>
    public static void ConfigureJsonbDictionary<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyDictionary<string, string?>>> propertyExpression,
        string columnName = "attributes",
        bool createGinIndex = true
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        var converter = new ValueConverter<IReadOnlyDictionary<string, string?>, string>(
            static dict => SerializeNullableDictionary(dict),
            static json => DeserializeNullableDictionary(json)
        );

        var comparer = new ValueComparer<IReadOnlyDictionary<string, string?>>(
            static (left, right) => DictionariesEqual(left, right),
            static dict => ComputeDictionaryHashCode(dict),
            static dict => CloneNullableDictionary(dict)
        );

        var property = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .IsRequired();
        property.Metadata.SetValueComparer(comparer);

        if (createGinIndex)
        {
            _ = builder.HasIndex(property.Metadata.Name).HasMethod("gin");
        }
    }

    /// <summary>
    /// Maps a non-nullable string dictionary (e.g. qualifiers) to a PostgreSQL JSONB column.
    /// </summary>
    public static void ConfigureJsonbStringMap<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyDictionary<string, string>>> propertyExpression,
        string columnName
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        var converter = new ValueConverter<IReadOnlyDictionary<string, string>, string>(
            static dict => JsonSerializer.Serialize(dict, _jsonOptions),
            static json => DeserializeStringDictionary(json)
        );

        var comparer = new ValueComparer<IReadOnlyDictionary<string, string>>(
            static (left, right) => StringDictionariesEqual(left, right),
            static dict => ComputeStringDictionaryHashCode(dict),
            static dict => new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase)
        );

        var property = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .IsRequired();
        property.Metadata.SetValueComparer(comparer);
    }

    private static string SerializeNullableDictionary(IReadOnlyDictionary<string, string?> dict)
    {
        return JsonSerializer.Serialize(dict, _jsonOptions);
    }

    private static Dictionary<string, string?> DeserializeNullableDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(json, _jsonOptions);
        return parsed is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(parsed, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> DeserializeStringDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
        return parsed is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
    }

    private static bool DictionariesEqual(
        IReadOnlyDictionary<string, string?>? left,
        IReadOnlyDictionary<string, string?>? right
    )
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (
                !right.TryGetValue(pair.Key, out var otherValue)
                || !string.Equals(pair.Value, otherValue, StringComparison.Ordinal)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static bool StringDictionariesEqual(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right
    )
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (
                !right.TryGetValue(pair.Key, out var otherValue)
                || !string.Equals(pair.Value, otherValue, StringComparison.Ordinal)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static int ComputeDictionaryHashCode(IReadOnlyDictionary<string, string?> dict)
    {
        var hash = new HashCode();
        foreach (var pair in dict.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(pair.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(pair.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private static int ComputeStringDictionaryHashCode(IReadOnlyDictionary<string, string> dict)
    {
        var hash = new HashCode();
        foreach (var pair in dict.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(pair.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(pair.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private static Dictionary<string, string?> CloneNullableDictionary(
        IReadOnlyDictionary<string, string?> dict
    )
    {
        return new Dictionary<string, string?>(dict, StringComparer.OrdinalIgnoreCase);
    }
}
