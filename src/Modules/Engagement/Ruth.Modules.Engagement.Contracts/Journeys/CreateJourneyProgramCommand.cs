using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Command to create a JourneyProgram.</summary>
/// <param name="Spec">Journey program specification.</param>
public sealed record CreateJourneyProgramCommand(JourneyProgramSpec Spec)
    : ICommand<JourneyProgramResponse>;
