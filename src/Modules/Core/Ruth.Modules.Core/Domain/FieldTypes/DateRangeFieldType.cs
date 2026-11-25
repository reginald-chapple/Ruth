using System.Globalization;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class DateRangeFieldType : FieldTypePluginBase
{
    public override string Key => "daterange";
    public override Guid SystemGuid =>
        new(
            0xa75dfcd5,
            0x277,
            0x44a4,
            0x90,
            0x7e,
            0x7b,
            0x9d,
            0x4c,
            0x25,
            0xe0,
            0x6
        ) /* a75dfcd5-0277-44a4-907e-7b9d4c25e006 */
    ;
    public override string Name => "Date Range";
    public override string Description =>
        "Start and end date pair stored as yyyy-MM-dd|yyyy-MM-dd.";
    public override FieldEditorKind EditorKind => FieldEditorKind.DateRange;

    protected override Result<string?> ValidateNonEmpty(
        string value,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        var parts = value.Split('|', count: 2);
        if (parts.Length != 2)
        {
            return ValidationError("Invalid", "Date range must be formatted as start|end.");
        }

        DateOnly? start = null;
        DateOnly? end = null;
        if (!string.IsNullOrWhiteSpace(parts[0]))
        {
            if (
                !DateOnly.TryParse(
                    parts[0].Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedStart
                )
            )
            {
                return ValidationError("InvalidStart", "Start date is invalid.");
            }

            start = parsedStart;
        }

        if (!string.IsNullOrWhiteSpace(parts[1]))
        {
            if (
                !DateOnly.TryParse(
                    parts[1].Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedEnd
                )
            )
            {
                return ValidationError("InvalidEnd", "End date is invalid.");
            }

            end = parsedEnd;
        }

        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            return ValidationError("Order", "Start date cannot be after end date.");
        }

        var normalizedStart = start?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            ?? string.Empty;
        var normalizedEnd = end?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            ?? string.Empty;
        return Result.Success<string?>($"{normalizedStart}|{normalizedEnd}");
    }

    public override string FormatDisplayValue(
        string? storedValue,
        IReadOnlyDictionary<string, string> qualifiers
    )
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return string.Empty;
        }

        var parts = storedValue.Split('|', count: 2);
        return parts.Length == 2 ? $"{parts[0]} to {parts[1]}".Trim() : storedValue;
    }
}
