using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Query to list configured JourneyPrograms.</summary>
/// <param name="ActiveOnly">Whether to return only active programs.</param>
public sealed record GetJourneyProgramsQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<JourneyProgramResponse>>;
