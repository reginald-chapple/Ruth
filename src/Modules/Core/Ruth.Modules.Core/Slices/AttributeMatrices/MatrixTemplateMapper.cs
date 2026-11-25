using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.AttributeMatrices;

internal static class MatrixTemplateMapper
{
    public static AttributeMatrixTemplateResponse ToResponse(AttributeMatrixTemplate entity)
    {
        return new AttributeMatrixTemplateResponse(
            entity.Id.Value,
            entity.SystemGuid,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.MinimumRows,
            entity.MaximumRows,
            entity.FormattedTemplate,
            [
                .. entity
                    .Columns.OrderBy(static c => c.Order)
                    .Select(static c => new MatrixColumnDefinitionDto(
                        c.Key,
                        c.Name,
                        c.FieldTypeKey,
                        c.IsRequired,
                        c.Order,
                        c.DefaultValue,
                        new Dictionary<string, string>(
                            c.Qualifiers,
                            StringComparer.OrdinalIgnoreCase
                        )
                    )),
            ]
        );
    }

    public static IReadOnlyList<MatrixColumnDefinition> ToDomainColumns(
        IReadOnlyList<MatrixColumnDefinitionDto> dtos
    )
    {
        return
        [
            .. dtos.Select(static c => new MatrixColumnDefinition(
                c.Key,
                c.Name,
                c.FieldTypeKey,
                c.IsRequired,
                c.Order,
                c.DefaultValue,
                c.Qualifiers is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(c.Qualifiers, StringComparer.OrdinalIgnoreCase)
            )),
        ];
    }
}
