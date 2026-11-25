namespace Ruth.Modules.Core.Contracts.FieldTypes;

/// <summary>Describes a configuration qualifier supported by an attribute field type.</summary>
public sealed record FieldQualifierDefinitionResponse(
    string Key,
    string Name,
    string Description,
    string? DefaultValue = null
);
