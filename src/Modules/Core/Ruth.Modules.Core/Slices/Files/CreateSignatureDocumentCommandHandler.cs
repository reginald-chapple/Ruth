using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class CreateSignatureDocumentCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateSignatureDocumentCommand, SignatureDocumentResponse>
{
    public async Task<Result<SignatureDocumentResponse>> HandleAsync(
        CreateSignatureDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var templateId = SignatureDocumentTemplateId.From(command.SignatureDocumentTemplateId);
        var template = await dbContext
            .SignatureDocumentTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (template is null)
        {
            return CoreDocumentErrors.SignatureTemplateNotFound;
        }

        var created = SignatureDocument.Create(
            templateId,
            command.Name,
            command.AppliesToEntityId,
            command.AssignedToUserId,
            command.DocumentKey,
            command.RenderedDocumentText ?? template.LavaTemplate
        );
        if (created.IsFailure)
        {
            return Result.Failure<SignatureDocumentResponse>(created.Errors);
        }

        _ = dbContext.SignatureDocuments.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(created.Value));
    }
}
