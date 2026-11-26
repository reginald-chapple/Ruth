using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CmsPageBlocksLoader(CmsDbContext db)
{
    public async Task<(
        IReadOnlyDictionary<string, IReadOnlyList<CmsBlockResponse>> BlocksByZone,
        IReadOnlyDictionary<Guid, HtmlContentResponse> ActiveHtml
    )> LoadAsync(
        CmsSiteId siteId,
        CmsLayoutId layoutId,
        CmsPageId pageId,
        DateTimeOffset nowUtc,
        CancellationToken ct
    )
    {
        var blocks = await db
            .Blocks.AsNoTracking()
            .Where(b =>
                b.IsActive && (b.PageId == pageId || b.LayoutId == layoutId || b.SiteId == siteId)
            )
            .OrderBy(static b => b.Zone)
            .ThenBy(static b => b.Order)
            .ToListAsync(ct);

        var blockTypes = await db
            .BlockTypes.AsNoTracking()
            .ToDictionaryAsync(static bt => bt.Id, ct);

        var grouped = new Dictionary<string, List<CmsBlockResponse>>(
            StringComparer.OrdinalIgnoreCase
        );
        List<CmsBlockId> blockIds = [];
        foreach (var block in blocks)
        {
            if (!blockTypes.TryGetValue(block.BlockTypeId, out var bt))
            {
                continue;
            }

            if (!grouped.TryGetValue(block.Zone, out var zoneList))
            {
                zoneList = [];
                grouped[block.Zone] = zoneList;
            }

            zoneList.Add(CmsProjectionMapper.ToResponse(block, bt));
            blockIds.Add(block.Id);
        }

        var activeHtml = await LoadActiveHtmlAsync(blockIds, nowUtc, ct);
        var readonlyZones = grouped.ToDictionary(
            static kv => kv.Key,
            static kv => (IReadOnlyList<CmsBlockResponse>)kv.Value,
            StringComparer.OrdinalIgnoreCase
        );

        return (readonlyZones, activeHtml);
    }

    private async Task<IReadOnlyDictionary<Guid, HtmlContentResponse>> LoadActiveHtmlAsync(
        List<CmsBlockId> blockIds,
        DateTimeOffset nowUtc,
        CancellationToken ct
    )
    {
        var htmlRows = await db
            .HtmlContents.AsNoTracking()
            .Where(h => blockIds.Contains(h.BlockId) && h.IsApproved)
            .OrderByDescending(static h => h.Version)
            .ToListAsync(ct);

        var activeHtml = new Dictionary<Guid, HtmlContentResponse>();
        foreach (var row in htmlRows)
        {
            if (!activeHtml.ContainsKey(row.BlockId.Value) && row.IsActiveAt(nowUtc))
            {
                activeHtml[row.BlockId.Value] = CmsProjectionMapper.ToResponse(row);
            }
        }

        return activeHtml;
    }
}
