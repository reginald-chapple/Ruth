using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Reports;

namespace Ruth.Modules.Reporting.Infrastructure.Reports;

internal sealed class JsonbAttributeFieldSelectPlugin : IFieldSelectPlugin
{
    public const string PluginKey = "reporting.field.attribute";

    public string Key => PluginKey;
    public string DisplayName => "JSONB Attribute";
    public string Description =>
        "Projects a typed attribute key from the entity jsonb attribute bag.";

    public object? SelectValue(DataViewEntityRecord record, string? selectionConfigJson)
    {
        ArgumentNullException.ThrowIfNull(record);
        var attrKey = EntityPropertyFieldSelectPlugin.ReadConfigString(
            selectionConfigJson,
            "attributeKey"
        ) ?? EntityPropertyFieldSelectPlugin.ReadConfigString(selectionConfigJson, "key");
        return string.IsNullOrWhiteSpace(attrKey) || record.Attributes is null ? null
            : record.Attributes.TryGetValue(attrKey, out var value) ? value
            : null;
    }
}
