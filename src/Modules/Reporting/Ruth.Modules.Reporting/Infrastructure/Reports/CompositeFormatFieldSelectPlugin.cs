using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain.DataViews;

namespace Ruth.Modules.Reporting.Infrastructure.Reports;

internal sealed class CompositeFormatFieldSelectPlugin : IFieldSelectPlugin
{
    public const string PluginKey = "reporting.field.composite";

    public string Key => PluginKey;
    public string DisplayName => "Composite Expression";
    public string Description =>
        "Combines multiple entity properties into a single formatted string.";

    public object? SelectValue(DataViewEntityRecord record, string? selectionConfigJson)
    {
        ArgumentNullException.ThrowIfNull(record);
        var template = EntityPropertyFieldSelectPlugin.ReadConfigString(
            selectionConfigJson,
            "template"
        ) ?? "{FirstName} {LastName}";

        var result = template;
        foreach (var (key, value) in record.Values)
        {
            var token = $"{{{key}}}";
            if (result.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Replace(
                    token,
                    DataViewScalarComparer.FormatScalar(value),
                    StringComparison.OrdinalIgnoreCase
                );
            }
        }

        return result.Trim();
    }
}
