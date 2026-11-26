using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Datasets;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Datasets;

internal sealed class PersistedDatasetMaterializer(CmsDbContext db, IClock clock)
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RefreshDatasetAsync(
        PersistedDataset dataset,
        CancellationToken cancellationToken
    )
    {
        var sw = Stopwatch.StartNew();
        var now = clock.UtcNow;

        var json = dataset.DataSourceKind switch
        {
            PersistedDatasetSourceKind.StaticJson => ResolveStaticJson(dataset),
            PersistedDatasetSourceKind.ContentChannelFeed => await BuildContentChannelJsonAsync(
                dataset,
                now,
                cancellationToken
            ),
            PersistedDatasetSourceKind.SitePagesSummary => await BuildSitePagesJsonAsync(
                dataset,
                cancellationToken
            ),
            PersistedDatasetSourceKind.AdaptiveMessagesSummary =>
                await BuildAdaptiveMessagesJsonAsync(cancellationToken),
            PersistedDatasetSourceKind.MediaElementsFeed => await BuildMediaElementsJsonAsync(
                dataset,
                cancellationToken
            ),
            _ => "[]",
        };

        sw.Stop();
        dataset.RecordRefresh(json, sw.ElapsedMilliseconds, now);
    }

    private static string ResolveStaticJson(PersistedDataset dataset)
    {
        if (
            dataset.SourceConfiguration.TryGetValue("Json", out var raw)
            && !string.IsNullOrWhiteSpace(raw)
        )
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return doc.RootElement.GetRawText();
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new { raw }, _jsonOptions);
            }
        }

        return "{}";
    }

    private async Task<string> BuildContentChannelJsonAsync(
        PersistedDataset dataset,
        DateTimeOffset nowUtc,
        CancellationToken ct
    )
    {
        var channelSlug = dataset.SourceConfiguration.GetValueOrDefault("ChannelSlug");
        var q = db.ContentChannelItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(channelSlug))
        {
            var normalized = CmsTextNormalizer.NormalizeSlug(channelSlug);
            var ch = await db
                .ContentChannels.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == normalized, ct);
            if (ch is null)
            {
                return "[]";
            }

            q = q.Where(i => i.ContentChannelId == ch.Id);
        }

        var items = await q.OrderByDescending(static i => i.StartDateTimeUtc)
            .Take(50)
            .ToListAsync(ct);

        var published = items
            .Where(i => i.IsPublishedAt(nowUtc))
            .Select(static i => new
            {
                id = i.Id.Value,
                i.Title,
                i.Slug,
                i.Summary,
                i.StartDateTimeUtc,
                i.Priority,
            });

        return JsonSerializer.Serialize(published, _jsonOptions);
    }

    private async Task<string> BuildSitePagesJsonAsync(
        PersistedDataset dataset,
        CancellationToken ct
    )
    {
        var q = db
            .Pages.AsNoTracking()
            .Include(static p => p.Routes)
            .Where(static p => p.IsPublished);
        if (
            dataset.SourceConfiguration.TryGetValue("SiteSlug", out var slug)
            && !string.IsNullOrWhiteSpace(slug)
        )
        {
            var norm = CmsTextNormalizer.NormalizeSlug(slug);
            var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == norm, ct);
            if (site is null)
            {
                return "[]";
            }

            q = q.Where(p => p.SiteId == site.Id);
        }

        var pages = await q.OrderBy(static p => p.Order).ToListAsync(ct);
        var projected = pages.Select(static p => new
        {
            id = p.Id.Value,
            p.InternalName,
            p.PageTitle,
            routes = p.Routes.Select(static r => r.Route).ToList(),
        });

        return JsonSerializer.Serialize(projected, _jsonOptions);
    }

    private async Task<string> BuildAdaptiveMessagesJsonAsync(CancellationToken ct)
    {
        var messages = await db
            .AdaptiveMessages.AsNoTracking()
            .Include(static m => m.Adaptations)
            .Where(static m => m.IsActive)
            .OrderBy(static m => m.Name)
            .ToListAsync(ct);

        var projected = messages.Select(static m => new
        {
            id = m.Id.Value,
            m.Key,
            m.Name,
            adaptationCount = m.Adaptations.Count,
        });
        return JsonSerializer.Serialize(projected, _jsonOptions);
    }

    private async Task<string> BuildMediaElementsJsonAsync(
        PersistedDataset dataset,
        CancellationToken ct
    )
    {
        var q = db.MediaElements.AsNoTracking();
        if (
            dataset.SourceConfiguration.TryGetValue("MediaFolderId", out var rawFolderId)
            && Guid.TryParse(rawFolderId, out var parsed)
        )
        {
            var fid = MediaFolderId.From(parsed);
            q = q.Where(e => e.MediaFolderId == fid);
        }

        var list = await q.OrderByDescending(static e => e.PublishedDateTimeUtc)
            .Take(50)
            .Select(static e => new
            {
                id = e.Id.Value,
                e.Name,
                e.DurationSeconds,
                e.ThumbnailUrl,
                e.PlaybackUrl,
                e.HlsManifestUrl,
            })
            .ToListAsync(ct);

        return JsonSerializer.Serialize(list, _jsonOptions);
    }
}
