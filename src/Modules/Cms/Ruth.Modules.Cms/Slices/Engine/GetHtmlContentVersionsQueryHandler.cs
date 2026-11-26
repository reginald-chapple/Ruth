using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetHtmlContentVersionsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetHtmlContentVersionsQuery, IReadOnlyList<HtmlContentResponse>>
{
    public async Task<Result<IReadOnlyList<HtmlContentResponse>>> HandleAsync(
        GetHtmlContentVersionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var blockId = CmsBlockId.From(query.BlockId);
        var q = db.HtmlContents.AsNoTracking().Where(h => h.BlockId == blockId);
        if (!string.IsNullOrWhiteSpace(query.EntityValue))
        {
            q = q.Where(h => h.EntityValue == query.EntityValue);
        }

        var list = await q.OrderByDescending(static h => h.Version).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<HtmlContentResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
