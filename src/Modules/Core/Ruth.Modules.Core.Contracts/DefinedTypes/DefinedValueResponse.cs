namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Represents an individual lookup value within a DefinedType.</summary>
public sealed record DefinedValueResponse(
    Guid Id,
    Guid DefinedTypeId,
    Guid SystemGuid,
    string Value,
    string? Description,
    Guid? CategoryId,
    int Order,
    bool IsSystem,
    bool IsActive,
    IReadOnlyDictionary<string, string?> Attributes
);
