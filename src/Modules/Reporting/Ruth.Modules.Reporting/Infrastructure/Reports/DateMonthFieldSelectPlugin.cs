using System.Globalization;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain.DataViews;

namespace Ruth.Modules.Reporting.Infrastructure.Reports;

internal sealed class DateMonthFieldSelectPlugin : IFieldSelectPlugin
{
    public const string PluginKey = "reporting.field.date-month";

    public string Key => PluginKey;
    public string DisplayName => "Date Year-Month";
    public string Description => "Formats a date/timestamp property into YYYY-MM period buckets.";

    public object? SelectValue(DataViewEntityRecord record, string? selectionConfigJson)
    {
        ArgumentNullException.ThrowIfNull(record);
        var property = EntityPropertyFieldSelectPlugin.ReadConfigString(
            selectionConfigJson,
            "property"
        ) ?? "CreatedOnUtc";
        if (!record.Values.TryGetValue(property, out var raw) || raw is null)
        {
            return null;
        }

        var text = DataViewScalarComparer.FormatScalar(raw);
        return DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var dto
        )
            ? dto.ToUniversalTime().ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : null;
    }
}
