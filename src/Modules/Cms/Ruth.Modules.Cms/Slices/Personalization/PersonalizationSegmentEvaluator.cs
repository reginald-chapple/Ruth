using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class PersonalizationSegmentEvaluator(
    CmsDbContext db,
    IPersonalizationSegmentCache cache,
    IClock clock
)
{
    public async Task<int> EvaluateAndSyncAsync(
        PersonalizationSegment segment,
        CancellationToken ct
    )
    {
        var now = clock.UtcNow;
        var matchedPeople = new Dictionary<Guid, double>();

        if (segment.FilterSpec.ExplicitPersonIds is { Count: > 0 } explicitIds)
        {
            foreach (var personId in explicitIds.Where(static g => g != Guid.Empty))
            {
                matchedPeople[personId] = 1.0;
            }
        }

        await AddInteractionAffinityMatchesAsync(segment, now, matchedPeople, ct);

        var existing = await db
            .PersonalizationSegmentMemberships.Where(m => m.SegmentId == segment.Id)
            .ToListAsync(ct);
        db.PersonalizationSegmentMemberships.RemoveRange(existing);

        foreach (var (personId, score) in matchedPeople)
        {
            _ = db.PersonalizationSegmentMemberships.Add(
                new PersonalizationSegmentMembership(segment.Id, personId, now, score)
            );
        }

        segment.RecordEvaluation(matchedPeople.Count, now);
        _ = await db.SaveChangesAsync(ct);
        await cache.SetSegmentMembersAsync(segment.SegmentKey, [.. matchedPeople.Keys], ct);

        return matchedPeople.Count;
    }

    private async Task AddInteractionAffinityMatchesAsync(
        PersonalizationSegment segment,
        DateTimeOffset now,
        Dictionary<Guid, double> matchedPeople,
        CancellationToken ct
    )
    {
        var spec = segment.FilterSpec;
        if (spec.MinimumInteractionCount <= 0)
        {
            return;
        }

        var lookbackStart = now.AddDays(-Math.Clamp(spec.InteractionLookbackDays, 1, 3650));
        var q = db
            .Interactions.AsNoTracking()
            .Where(i => i.PersonId != null && i.OccurredDateTimeUtc >= lookbackStart);

        if (spec.InteractionChannelKeys is { Count: > 0 } rawKeys)
        {
            List<string> normalizedKeys =
            [
                .. rawKeys.Select(CmsTextNormalizer.NormalizeKey).Where(static k => k.Length > 0),
            ];
            var channelIds = await db
                .InteractionChannels.AsNoTracking()
                .Where(c => normalizedKeys.Contains(c.Key))
                .Select(static c => c.Id)
                .ToListAsync(ct);
            var compIds = await db
                .InteractionComponents.AsNoTracking()
                .Where(c => channelIds.Contains(c.InteractionChannelId))
                .Select(static c => c.Id)
                .ToListAsync(ct);
            q = q.Where(i => compIds.Contains(i.InteractionComponentId));
        }

        if (spec.RequiredOperations is { Count: > 0 } ops)
        {
            q = q.Where(i => ops.Contains(i.Operation));
        }

        var counts = await q.GroupBy(static i => i.PersonId!.Value)
            .Select(static g => new { PersonId = g.Key, Count = g.Count() })
            .Where(x => x.Count >= spec.MinimumInteractionCount)
            .ToListAsync(ct);

        foreach (var entry in counts)
        {
            var score = Math.Min(10.0, entry.Count / (double)spec.MinimumInteractionCount);
            matchedPeople[entry.PersonId] = Math.Max(
                matchedPeople.GetValueOrDefault(entry.PersonId),
                score
            );
        }
    }
}
