using System.Globalization;
using Ruth.Modules.Workflow.Contracts.Forms;

namespace Ruth.Modules.Workflow.Domain.Forms;

internal static class FormVisibilityPredicateHelper
{
    public static bool IsFieldCurrentlyVisible(
        WorkflowActionFormField field,
        IReadOnlyDictionary<string, bool> sectionVisibility,
        IReadOnlyDictionary<string, string?> effective
    )
    {
        var secVisible =
            string.IsNullOrWhiteSpace(field.SectionKey)
            || !sectionVisibility.TryGetValue(field.SectionKey, out var sv)
            || sv;

        return field.IsVisible
            && secVisible
            && FormVisibilityEvaluator.IsRuleSatisfied(field.VisibilityRule, effective);
    }

    public static bool IsTypeValid(WorkflowActionFormField field, string value)
    {
        return field.FieldTypeKey switch
        {
            "email" => value.Contains('@', StringComparison.Ordinal) && value.Length >= 5,
            "number" or "currency" or "rating" => decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out _
            ),
            "date" => IsDateValueValid(value),
            "select" => IsSelectValueValid(field.SelectOptions, value),
            _ => true,
        };
    }

    public static bool EvaluateCondition(
        FieldVisibilityConditionDto cond,
        IReadOnlyDictionary<string, string?> values
    )
    {
        var key = cond.SourceAttributeKey ?? string.Empty;
        _ = values.TryGetValue(key, out var raw);
        return DispatchOperator(cond.Operator, raw?.Trim(), cond.ComparisonValue?.Trim());
    }

    public static void CopyInto(
        Dictionary<string, string?> destination,
        IReadOnlyDictionary<string, string?>? source
    )
    {
        if (source is null)
        {
            return;
        }

        foreach (var (key, val) in source)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                destination[key.Trim()] = val;
            }
        }
    }

    private static bool DispatchOperator(
        FieldVisibilityOperator op,
        string? trimmed,
        string? expected
    )
    {
        return op switch
        {
            FieldVisibilityOperator.Equals => string.Equals(
                trimmed,
                expected,
                StringComparison.OrdinalIgnoreCase
            ),
            FieldVisibilityOperator.NotEquals => !string.Equals(
                trimmed,
                expected,
                StringComparison.OrdinalIgnoreCase
            ),
            FieldVisibilityOperator.Contains => ContainsIgnoreCase(trimmed, expected),
            FieldVisibilityOperator.IsEmpty => string.IsNullOrWhiteSpace(trimmed),
            FieldVisibilityOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(trimmed),
            FieldVisibilityOperator.GreaterThan => CompareDecimal(
                trimmed,
                expected,
                static (a, b) => a > b
            ),
            FieldVisibilityOperator.LessThan => CompareDecimal(trimmed, expected, static (a, b) =>
                a < b
            ),
            FieldVisibilityOperator.IsTrue => IsTruthy(trimmed),
            FieldVisibilityOperator.IsFalse => !IsTruthy(trimmed),
            _ => false,
        };
    }

    private static bool ContainsIgnoreCase(string? trimmed, string? expected)
    {
        return !string.IsNullOrEmpty(trimmed)
            && !string.IsNullOrEmpty(expected)
            && trimmed.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDateValueValid(string value)
    {
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, out _)
            || DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsSelectValueValid(IReadOnlyList<string> options, string value)
    {
        return options.Count == 0 || options.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    private static bool CompareDecimal(
        string? left,
        string? right,
        Func<decimal, decimal, bool> predicate
    )
    {
        return decimal.TryParse(left, NumberStyles.Number, CultureInfo.InvariantCulture, out var a)
            && decimal.TryParse(right, NumberStyles.Number, CultureInfo.InvariantCulture, out var b)
            && predicate(a, b);
    }

    private static bool IsTruthy(string? val)
    {
        return string.Equals(val, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(val, "1", StringComparison.Ordinal)
            || string.Equals(val, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
