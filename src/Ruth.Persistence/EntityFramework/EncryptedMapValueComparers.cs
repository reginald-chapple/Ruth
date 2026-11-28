using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ruth.Persistence.EntityFramework;

/// <summary>EF Core <see cref="ValueComparer{T}" /> instances for encrypted dictionary properties.</summary>
internal static class EncryptedMapValueComparers
{
    public static readonly ValueComparer<IReadOnlyDictionary<string, string>> StringMapComparer =
        new(
            static (left, right) => StringMapsEqual(left, right),
            static dict => ComputeStringMapHash(dict),
            static dict => new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase)
        );

    public static readonly ValueComparer<IReadOnlyDictionary<string, string?>> NullableMapComparer =
        new(
            static (left, right) => NullableMapsEqual(left, right),
            static dict => ComputeNullableMapHash(dict),
            static dict => new Dictionary<string, string?>(dict, StringComparer.OrdinalIgnoreCase)
        );

    private static bool StringMapsEqual(
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
                !right.TryGetValue(pair.Key, out var other)
                || !string.Equals(pair.Value, other, StringComparison.Ordinal)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static bool NullableMapsEqual(
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
                !right.TryGetValue(pair.Key, out var other)
                || !string.Equals(pair.Value, other, StringComparison.Ordinal)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static int ComputeStringMapHash(IReadOnlyDictionary<string, string> dict)
    {
        var hash = new HashCode();
        foreach (var pair in dict.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(pair.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(pair.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private static int ComputeNullableMapHash(IReadOnlyDictionary<string, string?> dict)
    {
        var hash = new HashCode();
        foreach (var pair in dict.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(pair.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(pair.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
