using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Cms.Infrastructure;

internal sealed class CmsTenantSeedContributor(CmsDbContext dbContext) : ITenantSeedContributor
{
    public int Order => 80;

    private static readonly (
        string Key,
        string Name,
        string Category,
        string Description,
        bool Interactive
    )[] _builtInBlockTypes =
    [
        (
            "cms.html-content",
            "HTML Content",
            "CMS",
            "Renders versioned, approved HTML & safe merge-token markup.",
            false
        ),
        (
            "cms.channel-feed",
            "Content Channel List",
            "CMS",
            "Lists published items from a CMS ContentChannel.",
            false
        ),
        (
            "cms.persisted-dataset",
            "Persisted Dataset View",
            "CMS",
            "Renders cached JSON output from a PersistedDataset.",
            false
        ),
        (
            "cms.adaptive-message",
            "Adaptive Message Banner",
            "Personalization",
            "Displays the highest-priority segment-targeted adaptation.",
            true
        ),
        (
            "cms.media-gallery",
            "Media Gallery",
            "Media",
            "Displays synced Vimeo/YouTube/HLS MediaElements.",
            true
        ),
        (
            "search.content-search",
            "Content Search",
            "Search",
            "PostgreSQL full-text and trigram content search block for public portals.",
            true
        ),
    ];

    public Task SeedTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return EnsureBuiltInBlockTypesAsync(dbContext, cancellationToken);
    }

    internal static async Task EnsureBuiltInBlockTypesAsync(
        CmsDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var existingKeys = await dbContext
            .BlockTypes.AsNoTracking()
            .Select(static b => b.Key)
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var (key, name, category, desc, interactive) in _builtInBlockTypes)
        {
            if (existingSet.Contains(key))
            {
                continue;
            }

            var res = CmsBlockType.Create(
                key,
                name,
                category,
                desc,
                requiresInteractivity: interactive,
                isSystem: true
            );
            if (res.IsSuccess)
            {
                _ = dbContext.BlockTypes.Add(res.Value);
                added = true;
            }
        }

        if (added)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
