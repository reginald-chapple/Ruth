using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class CreateDocumentTypeCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateDocumentTypeCommand, DocumentTypeResponse>
{
    public async Task<Result<DocumentTypeResponse>> HandleAsync(
        CreateDocumentTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var entityTypeId = EntityTypeDescriptorId.From(command.EntityTypeId);
        var fileTypeId = BinaryFileTypeId.From(command.BinaryFileTypeId);

        var fileTypeExists = await dbContext.BinaryFileTypes.AnyAsync(
            t => t.Id == fileTypeId,
            cancellationToken
        );
        if (!fileTypeExists)
        {
            return CoreDocumentErrors.BinaryFileTypeNotFound;
        }

        var created = DocumentType.Create(
            command.Name,
            entityTypeId,
            fileTypeId,
            command.EntityTypeQualifierColumn,
            command.EntityTypeQualifierValue,
            command.UserSelectable,
            command.IconName,
            command.Order,
            command.DefaultDocumentNameTemplate,
            command.MaxDocumentsPerEntity,
            command.IsImage
        );
        if (created.IsFailure)
        {
            return Result.Failure<DocumentTypeResponse>(created.Errors);
        }

        _ = dbContext.DocumentTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(created.Value));
    }
}
