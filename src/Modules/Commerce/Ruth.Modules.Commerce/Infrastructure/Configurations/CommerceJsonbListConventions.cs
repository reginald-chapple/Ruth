using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal static class CommerceJsonbListConventions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static void ConfigureGuidList<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyList<Guid>>> propertyExpression,
        string columnName
    )
        where TEntity : class
    {
        var converter = new ValueConverter<IReadOnlyList<Guid>, string>(
            static list => JsonSerializer.Serialize(list, _jsonOptions),
            static json => DeserializeGuidList(json)
        );

        var comparer = new ValueComparer<IReadOnlyList<Guid>>(
            static (left, right) => GuidListsEqual(left, right),
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

    private static List<Guid> DeserializeGuidList(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<Guid>>(json, _jsonOptions) ?? [];
    }

    private static List<Guid> CloneList(IReadOnlyList<Guid> source)
    {
        return [.. source];
    }

    private static bool GuidListsEqual(IReadOnlyList<Guid>? left, IReadOnlyList<Guid>? right)
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
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int ComputeHash(IReadOnlyList<Guid> list)
    {
        var hash = new HashCode();
        foreach (var item in list)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}
