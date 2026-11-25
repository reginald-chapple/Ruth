using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.AttributeMatrices;

internal sealed class UpdateAttributeMatrixTemplateCommandHandler(
    CoreDbContext dbContext,
    IFieldTypeRegistry fieldTypes
) : ICommandHandler<UpdateAttributeMatrixTemplateCommand, AttributeMatrixTemplateResponse>
{
    public async Task<Result<AttributeMatrixTemplateResponse>> HandleAsync(
        UpdateAttributeMatrixTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = AttributeMatrixTemplateId.From(command.TemplateId);
        var entity = await dbContext.AttributeMatrixTemplates.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CoreErrors.MatrixTemplateNotFound;
        }

        var updated = entity.Update(
            fieldTypes,
            command.Name,
            command.Description,
            command.IsActive,
            command.MinimumRows,
            command.MaximumRows,
            command.FormattedTemplate,
            MatrixTemplateMapper.ToDomainColumns(command.Columns)
        );
        if (updated.IsFailure)
        {
            return Result.Failure<AttributeMatrixTemplateResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return MatrixTemplateMapper.ToResponse(entity);
    }
}
