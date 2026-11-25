namespace Ruth.Modules.Core.Domain.FieldTypes;

/// <summary>Describes a configuration qualifier key accepted by an <see cref="IFieldTypePlugin" />.</summary>
public sealed record FieldQualifierDefinition(
    string Key,
    string Name,
    string Description,
    string? DefaultValue = null
);
