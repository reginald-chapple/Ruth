using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Query to list personalization segments.</summary>
public sealed record GetPersonalizationSegmentsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<PersonalizationSegmentResponse>>;
