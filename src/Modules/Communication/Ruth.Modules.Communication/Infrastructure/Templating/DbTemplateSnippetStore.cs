using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Domain.Templates;

namespace Ruth.Modules.Communication.Infrastructure.Templating;

internal sealed class DbTemplateSnippetStore(CommunicationDbContext dbContext)
    : ITemplateSnippetStore
{
    public async Task<TemplateSnippetDefinition?> FindActiveByKeyAsync(
        string key,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = TemplateSnippet.NormalizeKey(key);
        if (normalized.IsFailure)
        {
            return null;
        }

        var snippet = await dbContext
            .TemplateSnippets.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == normalized.Value && s.IsActive, cancellationToken);

        return snippet?.ToDefinition();
    }
}
