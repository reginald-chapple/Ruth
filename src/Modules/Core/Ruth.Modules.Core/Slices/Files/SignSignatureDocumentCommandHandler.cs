using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class SignSignatureDocumentCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<SignSignatureDocumentCommand, SignatureDocumentResponse>
{
    public async Task<Result<SignatureDocumentResponse>> HandleAsync(
        SignSignatureDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = SignatureDocumentId.From(command.SignatureDocumentId);
        var document = await dbContext.SignatureDocuments.FirstOrDefaultAsync(
            d => d.Id == id,
            cancellationToken
        );
        if (document is null)
        {
            return CoreDocumentErrors.SignatureDocumentNotFound;
        }

        var template = await dbContext
            .SignatureDocumentTemplates.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == document.SignatureDocumentTemplateId,
                cancellationToken
            );
        if (template is null)
        {
            return CoreDocumentErrors.SignatureTemplateNotFound;
        }

        var signResult = document.Sign(
            template.SignatureType,
            command.SignedName,
            command.SignedByEmail,
            command.SignedByUserId,
            command.DrawnSignatureDataUrl,
            command.SignedDocumentText,
            command.ClientIp,
            command.ClientUserAgent,
            clock.UtcNow
        );
        if (signResult.IsFailure)
        {
            return Result.Failure<SignatureDocumentResponse>(signResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(document));
    }
}
