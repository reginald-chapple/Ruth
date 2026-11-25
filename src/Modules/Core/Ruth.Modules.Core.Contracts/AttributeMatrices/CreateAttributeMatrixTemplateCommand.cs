using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.AttributeMatrices;

/// <summary>Creates a new attribute matrix template with typed column definitions.</summary>
public sealed record CreateAttributeMatrixTemplateCommand(
    string Name,
    string? Description,
    int? MinimumRows,
    int? MaximumRows,
    string? FormattedTemplate,
    IReadOnlyList<MatrixColumnDefinitionDto> Columns
) : ICommand<AttributeMatrixTemplateResponse>;
