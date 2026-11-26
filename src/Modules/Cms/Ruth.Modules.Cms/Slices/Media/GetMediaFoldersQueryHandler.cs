using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Media;

internal sealed class GetMediaFoldersQueryHandler(CmsDbContext db)
    : IQueryHandler<GetMediaFoldersQuery, IReadOnlyList<MediaFolderResponse>>
{
    public async Task<Result<IReadOnlyList<MediaFolderResponse>>> HandleAsync(
        GetMediaFoldersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.MediaFolders.AsNoTracking();
        if (query.MediaAccountId is { } aid)
        {
            var accountId = MediaAccountId.From(aid);
            q = q.Where(f => f.MediaAccountId == accountId);
        }

        var folders = await q.OrderBy(static f => f.Name).ToListAsync(cancellationToken);
        var counts = await db
            .MediaElements.AsNoTracking()
            .GroupBy(static e => e.MediaFolderId)
            .Select(static g => new { FolderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.FolderId, static x => x.Count, cancellationToken);

        return Result.Success<IReadOnlyList<MediaFolderResponse>>([
            .. folders.Select(f => new MediaFolderResponse(
                f.Id.Value,
                f.MediaAccountId.Value,
                f.Name,
                f.ExternalFolderKey,
                f.Description,
                f.ContentChannelId?.Value,
                f.IsPublic,
                f.IsContentChannelSyncEnabled,
                counts.GetValueOrDefault(f.Id, 0)
            )),
        ]);
    }
}
