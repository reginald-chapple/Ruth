using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Media;

internal sealed class GetMediaElementsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetMediaElementsQuery, IReadOnlyList<MediaElementResponse>>
{
    public async Task<Result<IReadOnlyList<MediaElementResponse>>> HandleAsync(
        GetMediaElementsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.MediaElements.AsNoTracking();
        if (query.MediaFolderId is { } fid)
        {
            var folderId = MediaFolderId.From(fid);
            q = q.Where(e => e.MediaFolderId == folderId);
        }
        else if (query.MediaAccountId is { } aid)
        {
            var accountId = MediaAccountId.From(aid);
            var folderIds = await db
                .MediaFolders.AsNoTracking()
                .Where(f => f.MediaAccountId == accountId)
                .Select(static f => f.Id)
                .ToListAsync(cancellationToken);
            q = q.Where(e => folderIds.Contains(e.MediaFolderId));
        }

        var list = await q.OrderByDescending(static e => e.PublishedDateTimeUtc)
            .ThenBy(static e => e.Name)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MediaElementResponse>>([
            .. list.Select(CmsContentProjectionMapper.ToResponse),
        ]);
    }
}
