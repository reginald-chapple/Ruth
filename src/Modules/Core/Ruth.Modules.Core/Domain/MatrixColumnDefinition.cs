namespace Ruth.Modules.Core.Domain;

/// <summary>Column schema entry stored inside an <see cref="AttributeMatrixTemplate" /> JSONB payload.</summary>
public sealed record MatrixColumnDefinition(
    string Key,
    string Name,
    string FieldTypeKey,
    bool IsRequired,
    int Order,
    string? DefaultValue,
    IReadOnlyDictionary<string, string> Qualifiers
);
