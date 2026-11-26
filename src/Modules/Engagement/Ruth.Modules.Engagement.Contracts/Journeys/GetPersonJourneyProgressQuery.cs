using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Query to evaluate a constituent's progress across one or all JourneyPrograms.</summary>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="JourneyProgramId">Optional single JourneyProgram filter.</param>
public sealed record GetPersonJourneyProgressQuery(Guid PersonId, Guid? JourneyProgramId = null)
    : IQuery<IReadOnlyList<PersonJourneySummaryResponse>>;
