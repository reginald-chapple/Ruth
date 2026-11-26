using System.Globalization;
using Ruth.Modules.Reporting.Contracts.DataViews;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Typed comparison helpers for DataView AST scalar and jsonb attribute predicates.</summary>
public static class DataViewScalarComparer
{
    public static bool Evaluate(
        object? actual,
        DataViewFilterOperator op,
        string? expected,
        string? secondaryExpected
    )
    {
        var text = FormatScalar(actual);
        return op switch
        {
            DataViewFilterOperator.IsBlank => string.IsNullOrWhiteSpace(text),
            DataViewFilterOperator.IsNotBlank => !string.IsNullOrWhiteSpace(text),
            DataViewFilterOperator.Equals => CompareEquality(text, expected),
            DataViewFilterOperator.NotEquals => !CompareEquality(text, expected),
            DataViewFilterOperator.Contains => ContainsText(text, expected),
            DataViewFilterOperator.StartsWith => StartsWithText(text, expected),
            DataViewFilterOperator.EndsWith => EndsWithText(text, expected),
            DataViewFilterOperator.In => MatchesInSet(text, expected),
            DataViewFilterOperator.Between => MatchesBetween(text, expected, secondaryExpected),
            DataViewFilterOperator.GreaterThan => CompareOrdered(text, expected) > 0,
            DataViewFilterOperator.GreaterThanOrEqual => CompareOrdered(text, expected) >= 0,
            DataViewFilterOperator.LessThan => CompareOrdered(text, expected) < 0,
            DataViewFilterOperator.LessThanOrEqual => CompareOrdered(text, expected) <= 0,
            _ => false,
        };
    }

    public static string FormatScalar(object? value)
    {
        return value is null ? string.Empty
            : value is string s ? s
            : FormatNonStringScalar(value);
    }

    private static string FormatNonStringScalar(object value)
    {
        return value is bool b ? (b ? "true" : "false")
            : value is DateTimeOffset dto
                ? dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : value is DateTime dt
                ? dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : value is DateOnly d ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : value is IFormattable f ? f.ToString(format: null, CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
    }

    private static bool CompareEquality(string actual, string? expected)
    {
        return string.IsNullOrEmpty(expected) ? string.IsNullOrEmpty(actual)
            : TryCompareNumeric(actual, expected, out var numCmp) ? numCmp == 0
            : TryCompareDates(actual, expected, out var dateCmp) ? dateCmp == 0
            : string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsText(string actual, string? expected)
    {
        return !string.IsNullOrEmpty(expected)
            && actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWithText(string actual, string? expected)
    {
        return !string.IsNullOrEmpty(expected)
            && actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EndsWithText(string actual, string? expected)
    {
        return !string.IsNullOrEmpty(expected)
            && actual.EndsWith(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesInSet(string actual, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var parts = expected.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        foreach (var part in parts)
        {
            if (CompareEquality(actual, part))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesBetween(string actual, string? low, string? high)
    {
        var lowCmp = CompareOrdered(actual, low);
        var highCmp = CompareOrdered(actual, high);
        return lowCmp is >= 0 && highCmp is <= 0;
    }

    private static int? CompareOrdered(string actual, string? expected)
    {
        return string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected) ? null
            : TryCompareNumeric(actual, expected, out var numCmp) ? numCmp
            : TryCompareDates(actual, expected, out var dateCmp) ? dateCmp
            : string.Compare(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCompareNumeric(string actual, string expected, out int comparison)
    {
        if (
            decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out var a)
            && decimal.TryParse(
                expected,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var b
            )
        )
        {
            comparison = a.CompareTo(b);
            return true;
        }

        comparison = 0;
        return false;
    }

    private static bool TryCompareDates(string actual, string expected, out int comparison)
    {
        if (
            DateTimeOffset.TryParse(
                actual,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var a
            )
            && DateTimeOffset.TryParse(
                expected,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var b
            )
        )
        {
            comparison = a.ToUniversalTime().CompareTo(b.ToUniversalTime());
            return true;
        }

        comparison = 0;
        return false;
    }
}
