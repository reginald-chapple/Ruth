using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.AttributeMatrices;

internal sealed class CreateAttributeMatrixTemplateCommandHandler(
    CoreDbContext dbContext,
    IFieldTypeRegistry fieldTypes
) : ICommandHandler<CreateAttributeMatrixTemplateCommand, AttributeMatrixTemplateResponse>
{
    public async Task<Result<AttributeMatrixTemplateResponse>> HandleAsync(
        CreateAttributeMatrixTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = AttributeMatrixTemplate.Create(
            fieldTypes,
            command.Name,
            command.Description,
            command.MinimumRows,
            command.MaximumRows,
            command.FormattedTemplate,
            MatrixTemplateMapper.ToDomainColumns(command.Columns)
        );
        if (created.IsFailure)
        {
            return Result.Failure<AttributeMatrixTemplateResponse>(created.Errors);
        }

        _ = dbContext.AttributeMatrixTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return MatrixTemplateMapper.ToResponse(created.Value);
    }
}
