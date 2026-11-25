using System.Collections;
using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>Safely resolves read-only DTO paths and parses sample JSON contexts without exposing DbContext or reflection internals.</summary>
internal static class MergeValueResolver
{
    private const int MaxPropertyTraversalDepth = 8;

    private static readonly FrozenSet<Type> _safeScalarTypes = new HashSet<Type>
    {
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(Guid),
    }.ToFrozenSet();

    private static readonly string[] _forbiddenNamespacePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "StackExchange.Redis",
        "System.Reflection",
        "System.IO",
        "System.Net",
        "System.Threading",
        "System.Diagnostics",
    ];

    public static bool TryResolvePath(
        IReadOnlyDictionary<string, object?>? root,
        IReadOnlyList<string> segments,
        out object? value
    )
    {
        value = null;
        if (root is null || segments.Count == 0 || segments.Count > MaxPropertyTraversalDepth)
        {
            return false;
        }

        if (!TryReadDictionary(root, segments[0], out var current))
        {
            return false;
        }

        for (var i = 1; i < segments.Count; i++)
        {
            if (current is null || !TryStepInto(current, segments[i], out current))
            {
                value = null;
                return false;
            }
        }

        value = UnwrapJsonElement(current);
        return true;
    }

    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            ICollection collection => collection.Count > 0,
            _ => IsTruthyNumericOrObject(value),
        };
    }

    private static bool IsTruthyNumericOrObject(object value)
    {
        return value switch
        {
            int i => i != 0,
            long l => l != 0L,
            decimal d => d != 0m,
            double dbl => Math.Abs(dbl) > double.Epsilon,
            _ => true,
        };
    }

    public static Result<IReadOnlyDictionary<string, object?>> ParseJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind != JsonValueKind.Object
                ? CommunicationTemplateErrors.InvalidContextJson(
                    "Root JSON value must be an object."
                )
                : ConvertJsonObject(doc.RootElement);
        }
        catch (JsonException ex)
        {
            return CommunicationTemplateErrors.InvalidContextJson(ex.Message);
        }
    }

    private static bool TryStepInto(object current, string segment, out object? next)
    {
        next = null;

        if (current is IReadOnlyDictionary<string, object?> readOnlyDict)
        {
            return TryReadDictionary(readOnlyDict, segment, out next);
        }

        if (current is IReadOnlyDictionary<string, string> stringDict)
        {
            if (TryReadStringDictionary(stringDict, segment, out var strVal))
            {
                next = strVal;
                return true;
            }

            return false;
        }

        return current is JsonElement element
            ? TryReadJsonElement(element, segment, out next)
            : TryReadSafeDtoProperty(current, segment, out next);
    }

    private static bool TryReadDictionary(
        IReadOnlyDictionary<string, object?> dictionary,
        string key,
        out object? value
    )
    {
        if (dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var pair in dictionary)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryReadStringDictionary(
        IReadOnlyDictionary<string, string> dictionary,
        string key,
        out string? value
    )
    {
        if (dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var pair in dictionary)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryReadJsonElement(JsonElement element, string segment, out object? next)
    {
        next = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, segment, StringComparison.OrdinalIgnoreCase))
            {
                next = ConvertJsonValue(prop.Value);
                return true;
            }
        }

        return false;
    }

    private static bool TryReadSafeDtoProperty(object target, string segment, out object? next)
    {
        next = null;
        var type = target.GetType();
        if (!IsSafeDtoType(type))
        {
            return false;
        }

        var property = type.GetProperty(
            segment,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
        );
        if (property is null || !property.CanRead || property.GetIndexParameters().Length > 0)
        {
            return false;
        }

        if (!IsSafeDtoType(property.PropertyType))
        {
            return false;
        }

        next = property.GetValue(target);
        return true;
    }

    private static bool IsSafeDtoType(Type type)
    {
        var unwrapped = Nullable.GetUnderlyingType(type) ?? type;
        return unwrapped.IsPrimitive
            || unwrapped.IsEnum
            || _safeScalarTypes.Contains(unwrapped)
            || (!IsForbiddenRuntimeType(unwrapped) && !HasForbiddenNamespace(unwrapped.Namespace));
    }

    private static bool IsForbiddenRuntimeType(Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type)
            || typeof(MemberInfo).IsAssignableFrom(type)
            || typeof(IServiceProvider).IsAssignableFrom(type);
    }

    private static bool HasForbiddenNamespace(string? ns)
    {
        return !string.IsNullOrEmpty(ns)
            && _forbiddenNamespacePrefixes.Any(prefix =>
                ns.StartsWith(prefix, StringComparison.Ordinal)
            );
    }

    private static object? UnwrapJsonElement(object? value)
    {
        return value is JsonElement element ? ConvertJsonValue(element) : value;
    }

    private static Dictionary<string, object?> ConvertJsonObject(JsonElement obj)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in obj.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonValue(property.Value);
        }

        return dict;
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l
            : element.TryGetDecimal(out var d) ? d
            : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined or _ => null,
        };
    }
}
