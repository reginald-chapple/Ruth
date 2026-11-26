using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Cms.Infrastructure.Personalization;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class AdaptiveMessageContextLoader(
    CmsDbContext db,
    IPersonalizationSegmentCache segmentCache
)
{
    public async Task<HashSet<string>> ResolveMatchedSegmentsAsync(
        Guid? personId,
        RequestEvaluationContext? requestContext,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activeSegments = await db
            .PersonalizationSegments.AsNoTracking()
            .Where(static s => s.IsActive)
            .ToListAsync(ct);

        var filtersById = requestContext is null
            ? null
            : await db
                .RequestFilters.AsNoTracking()
                .Where(static f => f.IsActive)
                .ToDictionaryAsync(static f => f.Id, ct);

        foreach (var seg in activeSegments)
        {
            if (personId is { } pid && await IsPersonInSegmentAsync(seg, pid, ct))
            {
                _ = matched.Add(seg.SegmentKey);
                continue;
            }

            if (
                requestContext is not null
                && filtersById is not null
                && seg.FilterSpec.RequestFilterIds is { Count: > 0 } reqFilterIds
                && MatchesAnyRequestFilter(reqFilterIds, filtersById, requestContext, now)
            )
            {
                _ = matched.Add(seg.SegmentKey);
            }
        }

        return matched;
    }

    public async Task<Dictionary<Guid, int>> LoadRecentViewsAsync(
        AdaptiveMessage message,
        Guid? personId,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        var views = new Dictionary<Guid, int>();
        if (personId is null)
        {
            return views;
        }

        var saturatedCandidates = message
            .Adaptations.Where(static a => a.ViewSaturationCount is not null)
            .ToList();
        if (saturatedCandidates.Count == 0)
        {
            return views;
        }

        List<string> componentKeys =
        [
            .. saturatedCandidates.Select(static a => $"adaptation-{a.Id.Value:N}"),
        ];
        var components = await db
            .InteractionComponents.AsNoTracking()
            .Where(c => componentKeys.Contains(c.ComponentKey))
            .ToDictionaryAsync(
                static c => c.ComponentKey,
                static c => c.Id,
                StringComparer.OrdinalIgnoreCase,
                ct
            );

        foreach (var candidate in saturatedCandidates)
        {
            var compKey = $"adaptation-{candidate.Id.Value:N}";
            if (!components.TryGetValue(compKey, out var compId))
            {
                continue;
            }

            var lookback = now.AddDays(-(candidate.ViewSaturationInDays ?? 30));
            var count = await db
                .Interactions.AsNoTracking()
                .CountAsync(
                    i =>
                        i.InteractionComponentId == compId
                        && i.PersonId == personId
                        && i.OccurredDateTimeUtc >= lookback,
                    ct
                );
            views[candidate.Id.Value] = count;
        }

        return views;
    }

    private async Task<bool> IsPersonInSegmentAsync(
        PersonalizationSegment segment,
        Guid personId,
        CancellationToken ct
    )
    {
        var cached = await segmentCache.IsMemberAsync(segment.SegmentKey, personId, ct);
        return cached
            ?? await db
                .PersonalizationSegmentMemberships.AsNoTracking()
                .AnyAsync(m => m.SegmentId == segment.Id && m.PersonId == personId, ct);
    }

    private static bool MatchesAnyRequestFilter(
        IReadOnlyList<Guid> filterIds,
        Dictionary<RequestFilterId, RequestFilter> filtersById,
        RequestEvaluationContext ctx,
        DateTimeOffset now
    )
    {
        foreach (var fid in filterIds)
        {
            if (
                filtersById.TryGetValue(RequestFilterId.From(fid), out var filter)
                && RequestFilterEvaluator.Evaluate(filter, ctx, now).IsMatch
            )
            {
                return true;
            }
        }

        return false;
    }
}
