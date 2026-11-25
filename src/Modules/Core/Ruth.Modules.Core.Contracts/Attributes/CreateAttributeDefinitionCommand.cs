using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Creates a new global or entity-scoped attribute definition.</summary>
public sealed record CreateAttributeDefinitionCommand(
    Guid? EntityTypeId,
    string? EntityTypeQualifierColumn,
    string? EntityTypeQualifierValue,
    string Key,
    string Name,
    string? Description,
    string FieldTypeKey,
    bool IsRequired = false,
    bool IsGridColumn = false,
    bool IsMultiValue = false,
    string? DefaultValue = null,
    string? GlobalValue = null,
    int Order = 0,
    Guid? CategoryId = null,
    IReadOnlyDictionary<string, string>? Qualifiers = null
) : ICommand<AttributeDefinitionResponse>;
