using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Query to retrieve a JourneyProgram by GUID or URL slug.</summary>
/// <param name="IdOrSlug">GUID or URL slug.</param>
public sealed record GetJourneyProgramByIdOrSlugQuery(string IdOrSlug)
    : IQuery<JourneyProgramResponse>;
