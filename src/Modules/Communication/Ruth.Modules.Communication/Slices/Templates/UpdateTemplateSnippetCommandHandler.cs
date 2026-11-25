using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class UpdateTemplateSnippetCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<UpdateTemplateSnippetCommand, TemplateSnippetResponse>
{
    public async Task<Result<TemplateSnippetResponse>> HandleAsync(
        UpdateTemplateSnippetCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = TemplateSnippetId.From(command.SnippetId);
        var snippet = await dbContext.TemplateSnippets.FirstOrDefaultAsync(
            s => s.Id == id,
            cancellationToken
        );
        if (snippet is null)
        {
            return CommunicationTemplateErrors.SnippetNotFound;
        }

        var normalizedKey = TemplateSnippet.NormalizeKey(command.Key);
        if (normalizedKey.IsFailure)
        {
            return Result.Failure<TemplateSnippetResponse>(normalizedKey.Errors);
        }

        if (!string.Equals(snippet.Key, normalizedKey.Value, StringComparison.Ordinal))
        {
            var duplicate = await dbContext
                .TemplateSnippets.AsNoTracking()
                .AnyAsync(s => s.Key == normalizedKey.Value && s.Id != id, cancellationToken);
            if (duplicate)
            {
                return CommunicationTemplateErrors.SnippetKeyDuplicate;
            }
        }

        var updated = snippet.Update(
            command.Name,
            command.Key,
            command.Markup,
            command.Description,
            command.Documentation,
            command.OutputMode,
            command.DefaultParameters,
            command.CategoryIds,
            command.IsActive
        );
        if (updated.IsFailure)
        {
            return Result.Failure<TemplateSnippetResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(TemplateSnippetMapper.ToResponse(snippet));
    }
}
