using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class CreateDocumentCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateDocumentCommand, DocumentResponse>
{
    public async Task<Result<DocumentResponse>> HandleAsync(
        CreateDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var docTypeId = DocumentTypeId.From(command.DocumentTypeId);
        var docType = await dbContext
            .DocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == docTypeId, cancellationToken);
        if (docType is null)
        {
            return CoreDocumentErrors.DocumentTypeNotFound;
        }

        if (docType.MaxDocumentsPerEntity.HasValue)
        {
            var existingCount = await dbContext.Documents.CountAsync(
                d => d.DocumentTypeId == docTypeId && d.EntityId == command.EntityId,
                cancellationToken
            );
            if (existingCount >= docType.MaxDocumentsPerEntity.Value)
            {
                return CoreDocumentErrors.DocumentQuotaExceeded;
            }
        }

        var fileId = BinaryFileId.From(command.BinaryFileId);
        var binaryFile = await dbContext.BinaryFiles.FirstOrDefaultAsync(
            f => f.Id == fileId,
            cancellationToken
        );
        if (binaryFile is null)
        {
            return CoreDocumentErrors.BinaryFileNotFound;
        }

        if (!binaryFile.CanBeAttachedBy(command.RequestingUserId))
        {
            return CoreDocumentErrors.BinaryFileOwnershipDenied;
        }

        var created = Document.Create(
            docTypeId,
            command.EntityId,
            fileId,
            command.Name,
            command.PurposeKey,
            command.Description
        );
        if (created.IsFailure)
        {
            return Result.Failure<DocumentResponse>(created.Errors);
        }

        binaryFile.MarkPermanent();
        _ = dbContext.Documents.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(created.Value));
    }
}
