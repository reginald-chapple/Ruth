using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.AttributeMatrices;

internal sealed class DeleteAttributeMatrixTemplateCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteAttributeMatrixTemplateCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteAttributeMatrixTemplateCommand command,
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

        _ = dbContext.AttributeMatrixTemplates.Remove(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
