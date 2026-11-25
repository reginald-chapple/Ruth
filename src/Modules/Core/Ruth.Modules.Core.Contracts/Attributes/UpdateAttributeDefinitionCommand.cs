using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Updates an existing attribute definition.</summary>
public sealed record UpdateAttributeDefinitionCommand(
    Guid AttributeDefinitionId,
    string Name,
    string? Description,
    string FieldTypeKey,
    bool IsRequired,
    bool IsGridColumn,
    bool IsMultiValue,
    bool IsActive,
    string? DefaultValue,
    int Order,
    Guid? CategoryId,
    IReadOnlyDictionary<string, string>? Qualifiers = null
) : ICommand<AttributeDefinitionResponse>;
