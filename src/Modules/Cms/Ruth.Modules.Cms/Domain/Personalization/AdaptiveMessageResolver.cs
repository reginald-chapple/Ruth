namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Clean-room domain service selecting the highest-priority eligible adaptation with saturation enforcement.</summary>
public static class AdaptiveMessageResolver
{
    public static (AdaptiveMessageAdaptation? Adaptation, string Reason) SelectAdaptation(
        AdaptiveMessage message,
        IReadOnlySet<string> matchedSegmentKeys,
        IReadOnlyDictionary<Guid, int> recentViewsByAdaptationId,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(matchedSegmentKeys);
        ArgumentNullException.ThrowIfNull(recentViewsByAdaptationId);

        if (!message.IsWindowActiveAt(nowUtc))
        {
            return (null, "AdaptiveMessage is inactive or outside its schedule window.");
        }

        var ordered = message.Adaptations.OrderBy(static a => a.Order).ToList();
        var sawSaturated = false;

        foreach (var candidate in ordered)
        {
            if (!candidate.IsWindowActiveAt(nowUtc))
            {
                continue;
            }

            if (!MatchesSegments(candidate, matchedSegmentKeys))
            {
                continue;
            }

            if (IsSaturated(candidate, recentViewsByAdaptationId))
            {
                sawSaturated = true;
                continue;
            }

            var reason =
                candidate.SegmentKeys.Count == 0
                    ? "Matched default fallback adaptation."
                    : $"Matched prioritized segment adaptation '{candidate.Name}'.";
            return (candidate, reason);
        }

        return sawSaturated
            ? (null, "All matching adaptations exceeded their view saturation limit.")
            : (null, "No active adaptation matched the current viewer segments.");
    }

    private static bool MatchesSegments(
        AdaptiveMessageAdaptation candidate,
        IReadOnlySet<string> matchedSegmentKeys
    )
    {
        return candidate.SegmentKeys.Count == 0
            || candidate.SegmentKeys.Any(matchedSegmentKeys.Contains);
    }

    private static bool IsSaturated(
        AdaptiveMessageAdaptation candidate,
        IReadOnlyDictionary<Guid, int> recentViewsByAdaptationId
    )
    {
        return candidate.ViewSaturationCount is not null
            && recentViewsByAdaptationId.TryGetValue(candidate.Id.Value, out var views)
            && views >= candidate.ViewSaturationCount.Value;
    }
}
