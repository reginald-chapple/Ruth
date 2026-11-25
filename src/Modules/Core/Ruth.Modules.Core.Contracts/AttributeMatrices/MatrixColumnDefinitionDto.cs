namespace Ruth.Modules.Core.Contracts.AttributeMatrices;

/// <summary>Column schema definition inside an <see cref="AttributeMatrixTemplateResponse" />.</summary>
public sealed record MatrixColumnDefinitionDto(
    string Key,
    string Name,
    string FieldTypeKey,
    bool IsRequired = false,
    int Order = 0,
    string? DefaultValue = null,
    IReadOnlyDictionary<string, string>? Qualifiers = null
);
