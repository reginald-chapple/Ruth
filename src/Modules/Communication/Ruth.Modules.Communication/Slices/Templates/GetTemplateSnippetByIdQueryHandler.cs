using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class GetTemplateSnippetByIdQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetTemplateSnippetByIdQuery, TemplateSnippetResponse>
{
    public async Task<Result<TemplateSnippetResponse>> HandleAsync(
        GetTemplateSnippetByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = TemplateSnippetId.From(query.SnippetId);
        var snippet = await dbContext
            .TemplateSnippets.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return snippet is null
            ? CommunicationTemplateErrors.SnippetNotFound
            : Result.Success(TemplateSnippetMapper.ToResponse(snippet));
    }
}
