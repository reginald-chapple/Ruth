using System.Text.Json;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Reports;

namespace Ruth.Modules.Reporting.Infrastructure.Reports;

internal sealed class EntityPropertyFieldSelectPlugin : IFieldSelectPlugin
{
    public const string PluginKey = "reporting.field.property";

    public string Key => PluginKey;
    public string DisplayName => "Entity Property";
    public string Description => "Projects a direct scalar property from the entity record.";

    public object? SelectValue(DataViewEntityRecord record, string? selectionConfigJson)
    {
        ArgumentNullException.ThrowIfNull(record);
        var fieldName = ReadConfigString(selectionConfigJson, "property") ?? "EntityId";
        return string.Equals(fieldName, "EntityId", StringComparison.OrdinalIgnoreCase)
                ? record.EntityId
            : string.Equals(fieldName, "PersonId", StringComparison.OrdinalIgnoreCase)
                ? record.PersonId
            : record.Values.TryGetValue(fieldName, out var val) ? val
            : null;
    }

    internal static string? ReadConfigString(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (
                doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(key, out var prop)
                && prop.ValueKind == JsonValueKind.String
            )
            {
                return prop.GetString();
            }
        }
        catch (JsonException)
        {
            return json.Trim();
        }

        return null;
    }
}
