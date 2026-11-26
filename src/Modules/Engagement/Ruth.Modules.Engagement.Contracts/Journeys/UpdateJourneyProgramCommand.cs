using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Command to update an existing JourneyProgram.</summary>
/// <param name="JourneyProgramId">JourneyProgram identifier.</param>
/// <param name="Spec">Updated specification.</param>
public sealed record UpdateJourneyProgramCommand(Guid JourneyProgramId, JourneyProgramSpec Spec)
    : ICommand<JourneyProgramResponse>;
