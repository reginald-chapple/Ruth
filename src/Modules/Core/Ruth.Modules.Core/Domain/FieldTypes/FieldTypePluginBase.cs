using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal abstract class FieldTypePluginBase : IFieldTypePlugin
{
    public abstract string Key { get; }
    public abstract Guid SystemGuid { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract FieldEditorKind EditorKind { get; }
    public virtual IReadOnlyList<FieldQualifierDefinition> Qualifiers => [];

    public Result<string?> ValidateAndNormalize(
        string? rawValue,
        IReadOnlyDictionary<string, string> qualifiers,
        bool isRequired
    )
    {
        ArgumentNullException.ThrowIfNull(qualifiers);

        return string.IsNullOrWhiteSpace(rawValue)
            ? isRequired
                ? DomainError.Validation($"FieldType.{Key}.Required", $"{Name} value is required.")
                : Result.Success<string?>(value: null)
            : ValidateNonEmpty(rawValue.Trim(), qualifiers);
    }

    public virtual string FormatDisplayValue(
        string? storedValue,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        return storedValue ?? string.Empty;
    }

    protected abstract Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    );

    protected static string? GetQualifier(
        IReadOnlyDictionary<string, string> qualifiers,
        string key
    )
    {
        return qualifiers.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val)
            ? val.Trim()
            : null;
    }

    protected static int? GetIntQualifier(
        IReadOnlyDictionary<string, string> qualifiers,
        string key
    )
    {
        var raw = GetQualifier(qualifiers, key);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    protected static decimal? GetDecimalQualifier(
        IReadOnlyDictionary<string, string> qualifiers,
        string key
    )
    {
        var raw = GetQualifier(qualifiers, key);
        return decimal.TryParse(
            raw,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : null;
    }

    protected static bool GetBoolQualifier(
        IReadOnlyDictionary<string, string> qualifiers,
        string key,
        bool defaultValue = false
    )
    {
        var raw = GetQualifier(qualifiers, key);
        return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    protected static List<(string Value, string Label)> ParseSelectOptions(
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var raw = GetQualifier(qualifiers, "values");
        if (raw is null)
        {
            return [];
        }

        var options = new List<(string Value, string Label)>();
        foreach (
            var token in raw.Split(
                [',', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            var parts = token.Split('^', count: 2);
            var val = parts[0].Trim();
            var label =
                parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : val;
            if (!string.IsNullOrWhiteSpace(val))
            {
                options.Add((val, label));
            }
        }

        return options;
    }

    protected DomainError ValidationError(string codeSuffix, string message)
    {
        return DomainError.Validation($"FieldType.{Key}.{codeSuffix}", message);
    }
}
