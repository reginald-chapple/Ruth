namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Declarative criteria defining membership in a PersonalizationSegment.</summary>
public sealed record PersonalizationSegmentFilterSpec(
    IReadOnlyList<Guid>? RequestFilterIds = null,
    IReadOnlyList<string>? InteractionChannelKeys = null,
    IReadOnlyList<string>? RequiredOperations = null,
    int MinimumInteractionCount = 0,
    int InteractionLookbackDays = 30,
    string? DataViewSourceKey = null,
    IReadOnlyList<Guid>? ExplicitPersonIds = null,
    IReadOnlyList<string>? AffinityTags = null
);
