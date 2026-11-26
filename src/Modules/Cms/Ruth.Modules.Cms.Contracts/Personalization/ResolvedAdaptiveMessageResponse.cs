namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Result of resolving an AdaptiveMessage for a specific person and request snapshot.</summary>
public sealed record ResolvedAdaptiveMessageResponse(
    Guid AdaptiveMessageId,
    string AdaptiveMessageKey,
    AdaptiveMessageAdaptationResponse? SelectedAdaptation,
    IReadOnlyList<string> MatchedSegmentKeys,
    string ResolutionReason
);
