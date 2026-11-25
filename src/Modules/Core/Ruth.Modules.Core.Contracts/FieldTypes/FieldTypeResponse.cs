namespace Ruth.Modules.Core.Contracts.FieldTypes;

/// <summary>Metadata describing a registered modular field type plugin.</summary>
public sealed record FieldTypeResponse(
    string Key,
    Guid SystemGuid,
    string Name,
    string Description,
    FieldEditorKind EditorKind,
    IReadOnlyList<FieldQualifierDefinitionResponse> Qualifiers
);
