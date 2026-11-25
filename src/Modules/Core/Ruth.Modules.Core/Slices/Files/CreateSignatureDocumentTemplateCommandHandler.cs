using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class CreateSignatureDocumentTemplateCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateSignatureDocumentTemplateCommand, SignatureDocumentTemplateResponse>
{
    public async Task<Result<SignatureDocumentTemplateResponse>> HandleAsync(
        CreateSignatureDocumentTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = SignatureDocumentTemplate.Create(
            command.Name,
            command.Description,
            command.BinaryFileTypeId.HasValue
                ? BinaryFileTypeId.From(command.BinaryFileTypeId.Value)
                : null,
            command.DocumentTerm,
            command.TemplateMarkup,
            command.SignatureType,
            command.IsActive,
            command.IsValidInFuture,
            command.ValidityDurationInDays
        );
        if (created.IsFailure)
        {
            return Result.Failure<SignatureDocumentTemplateResponse>(created.Errors);
        }

        _ = dbContext.SignatureDocumentTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(created.Value));
    }
}
