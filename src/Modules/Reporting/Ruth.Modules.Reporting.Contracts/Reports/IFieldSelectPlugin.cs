using Ruth.Modules.Reporting.Contracts.DataViews;

namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Modular column projection plugin invoked when rendering Report columns.</summary>
public interface IFieldSelectPlugin
{
    /// <summary>Gets the unique plugin key (e.g., reporting.field.property, reporting.field.attribute).</summary>
    string Key { get; }

    /// <summary>Gets the human-readable display name.</summary>
    string DisplayName { get; }

    /// <summary>Gets the plugin description.</summary>
    string Description { get; }

    /// <summary>Evaluates the column value for a single entity record using the field configuration JSON.</summary>
    object? SelectValue(DataViewEntityRecord record, string? selectionConfigJson);
}
