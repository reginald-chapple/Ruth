using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class CreateTemplateSnippetCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<CreateTemplateSnippetCommand, TemplateSnippetResponse>
{
    public async Task<Result<TemplateSnippetResponse>> HandleAsync(
        CreateTemplateSnippetCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var keyResult = TemplateSnippet.NormalizeKey(command.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<TemplateSnippetResponse>(keyResult.Errors);
        }

        var exists = await dbContext
            .TemplateSnippets.AsNoTracking()
            .AnyAsync(s => s.Key == keyResult.Value, cancellationToken);
        if (exists)
        {
            return CommunicationTemplateErrors.SnippetKeyDuplicate;
        }

        var created = TemplateSnippet.Create(
            command.Name,
            command.Key,
            command.Markup,
            command.Description,
            command.Documentation,
            command.OutputMode,
            command.DefaultParameters,
            command.CategoryIds,
            isSystem: false,
            isActive: command.IsActive
        );
        if (created.IsFailure)
        {
            return Result.Failure<TemplateSnippetResponse>(created.Errors);
        }

        _ = dbContext.TemplateSnippets.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(TemplateSnippetMapper.ToResponse(created.Value));
    }
}
