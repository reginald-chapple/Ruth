namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Represents an extensible attribute definition (global, entity-type, or entity-qualifier scoped).</summary>
public sealed record AttributeDefinitionResponse(
    Guid Id,
    Guid SystemGuid,
    Guid? EntityTypeId,
    string? EntityTypeKey,
    string? EntityTypeQualifierColumn,
    string? EntityTypeQualifierValue,
    string Key,
    string Name,
    string? Description,
    string FieldTypeKey,
    bool IsRequired,
    bool IsGridColumn,
    bool IsMultiValue,
    bool IsSystem,
    bool IsActive,
    string? DefaultValue,
    string? GlobalValue,
    int Order,
    Guid? CategoryId,
    IReadOnlyDictionary<string, string> Qualifiers
);
