using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.AttributeMatrices;

/// <summary>Updates an existing attribute matrix template.</summary>
public sealed record UpdateAttributeMatrixTemplateCommand(
    Guid TemplateId,
    string Name,
    string? Description,
    bool IsActive,
    int? MinimumRows,
    int? MaximumRows,
    string? FormattedTemplate,
    IReadOnlyList<MatrixColumnDefinitionDto> Columns
) : ICommand<AttributeMatrixTemplateResponse>;
