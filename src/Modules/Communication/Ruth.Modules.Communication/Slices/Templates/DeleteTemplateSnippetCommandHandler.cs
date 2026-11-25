using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class DeleteTemplateSnippetCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<DeleteTemplateSnippetCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteTemplateSnippetCommand command,
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

        var canDelete = snippet.EnsureCanDelete();
        if (canDelete.IsFailure)
        {
            return Result.Failure<bool>(canDelete.Errors);
        }

        _ = dbContext.TemplateSnippets.Remove(snippet);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
