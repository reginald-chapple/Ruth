namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Rule determining when a constituent completes an entire JourneyProgram.</summary>
public enum JourneyCompletionMode
{
    /// <summary>Every active MilestoneType flagged IsRequiredForProgramCompletion must be completed.</summary>
    AllRequiredMilestones = 0,

    /// <summary>Completing any required milestone completes the program.</summary>
    AnyOneRequiredMilestone = 1,
}
