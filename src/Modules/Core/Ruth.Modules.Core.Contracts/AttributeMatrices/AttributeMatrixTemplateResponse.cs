namespace Ruth.Modules.Core.Contracts.AttributeMatrices;

/// <summary>Represents a repeatable structured Attribute Matrix template.</summary>
public sealed record AttributeMatrixTemplateResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string? Description,
    bool IsActive,
    int? MinimumRows,
    int? MaximumRows,
    string? FormattedTemplate,
    IReadOnlyList<MatrixColumnDefinitionDto> Columns
);
