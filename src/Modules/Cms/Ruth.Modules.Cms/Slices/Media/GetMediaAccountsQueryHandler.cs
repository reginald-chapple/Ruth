using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Media;

internal sealed class GetMediaAccountsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetMediaAccountsQuery, IReadOnlyList<MediaAccountResponse>>
{
    public async Task<Result<IReadOnlyList<MediaAccountResponse>>> HandleAsync(
        GetMediaAccountsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.MediaAccounts.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static a => a.IsActive);
        }

        var accounts = await q.OrderBy(static a => a.Name).ToListAsync(cancellationToken);
        var folders = await db.MediaFolders.AsNoTracking().ToListAsync(cancellationToken);
        var elements = await db
            .MediaElements.AsNoTracking()
            .GroupBy(static e => e.MediaFolderId)
            .Select(static g => new { FolderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.FolderId, static x => x.Count, cancellationToken);

        List<MediaAccountResponse> result = [];
        foreach (var acc in accounts)
        {
            var accFolders = folders.Where(f => f.MediaAccountId == acc.Id).ToList();
            var elemCount = accFolders.Sum(f => elements.GetValueOrDefault(f.Id, 0));
            result.Add(
                new MediaAccountResponse(
                    acc.Id.Value,
                    acc.Name,
                    acc.Slug,
                    acc.ProviderKey,
                    acc.AccountIdentifier,
                    acc.IsActive,
                    acc.LastSyncedUtc,
                    accFolders.Count,
                    elemCount
                )
            );
        }

        return Result.Success<IReadOnlyList<MediaAccountResponse>>(result);
    }
}
