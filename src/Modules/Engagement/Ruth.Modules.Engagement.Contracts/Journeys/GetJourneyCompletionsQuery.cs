using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Query to list JourneyCompletions filtered by program or person.</summary>
/// <param name="JourneyProgramId">Optional JourneyProgram filter.</param>
/// <param name="PersonId">Optional Person filter.</param>
public sealed record GetJourneyCompletionsQuery(
    Guid? JourneyProgramId = null,
    Guid? PersonId = null
) : IQuery<IReadOnlyList<JourneyCompletionResponse>>;
