using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class GetTemplateSnippetsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetTemplateSnippetsQuery, IReadOnlyList<TemplateSnippetResponse>>
{
    public async Task<Result<IReadOnlyList<TemplateSnippetResponse>>> HandleAsync(
        GetTemplateSnippetsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.TemplateSnippets.AsNoTracking().AsQueryable();

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            baseQuery = baseQuery.Where(s =>
                EF.Functions.ILike(s.Name, term) || EF.Functions.ILike(s.Key, term)
            );
        }

        var items = await baseQuery
            .OrderBy(static s => s.Name)
            .ThenBy(static s => s.Key)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TemplateSnippetResponse>>([
            .. items.Select(TemplateSnippetMapper.ToResponse),
        ]);
    }
}
